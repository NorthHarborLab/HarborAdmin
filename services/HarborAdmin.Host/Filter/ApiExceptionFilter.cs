using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace HarborAdmin.Host.Filter;

/// <summary>
/// 将常见业务异常映射为 <see cref="ApiResult"/> 与合适的 HTTP 状态码。
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private static readonly (Func<Exception, bool> Predicate, Func<Exception, ExceptionMap> Resolver)[] ExceptionMappings =
    [
        (exception => exception is AdminDomainException, exception => MapAdminDomainException((AdminDomainException)exception)),
        (exception => exception is KeyNotFoundException, ex => MapByCode(ApiResultCodes.NotFound, StatusCodes.Status404NotFound, ((KeyNotFoundException)ex).Message)),
        (exception => exception is UnauthorizedAccessException, _ => MapByCode(ApiResultCodes.Unauthorized, StatusCodes.Status401Unauthorized, "未授权。")),
        (exception => exception is ArgumentNullException, _ => MapByCode(ApiResultCodes.BadRequest, StatusCodes.Status400BadRequest, "请求参数不能为空。")),
        (exception => exception is ArgumentException, ex => MapByCode(ApiResultCodes.BadRequest, StatusCodes.Status400BadRequest, ((ArgumentException)ex).Message)),
        (exception => exception is InvalidOperationException, ex => MapByCode(ApiResultCodes.BadRequest, StatusCodes.Status400BadRequest, ((InvalidOperationException)ex).Message)),
        (exception => exception is ValidationException, ex => MapByCode(ApiResultCodes.BadRequest, StatusCodes.Status400BadRequest, ((ValidationException)ex).Message)),
        (exception => exception is FormatException, ex => MapByCode(ApiResultCodes.BadRequest, StatusCodes.Status400BadRequest, ((FormatException)ex).Message)),
        (exception => exception is JsonException, ex => MapByCode(ApiResultCodes.BadRequest, StatusCodes.Status400BadRequest, ((JsonException)ex).Message)),
        (exception => exception is PostgresException, ex => MapPostgresException((PostgresException)ex))
    ];

    /// <summary>
    /// 初始化统一异常过滤器。
    /// </summary>
    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        var mapping = ResolveException(context.Exception);
        var metadata = mapping.Metadata;
        if (mapping.StatusCode >= 500)
        {
            var traceId = context.HttpContext.TraceIdentifier;
            if (!string.IsNullOrWhiteSpace(traceId))
            {
                metadata = new
                {
                    TraceId = traceId,
                    Path = context.HttpContext.Request.Path.Value,
                    Method = context.HttpContext.Request.Method,
                    OriginalMeta = mapping.Metadata
                };
            }
            _logger.LogError(context.Exception, "Request processing failed. TraceId={TraceId}, Path={Path}, Method={Method}.",
                traceId,
                context.HttpContext.Request.Path.Value,
                context.HttpContext.Request.Method);
        }

        context.Result = new ObjectResult(ApiResult.Fail(
            mapping.Code,
            mapping.Message,
            mapping.Errors,
            metadata,
            mapping.Error.Code,
            mapping.Error.Kind.ToString(),
            mapping.Error.Arguments,
            mapping.Error.Retryable))
        {
            StatusCode = mapping.StatusCode,
        };
        context.ExceptionHandled = true;
    }

    private static ExceptionMap ResolveException(Exception exception)
    {
        foreach (var rule in ExceptionMappings)
        {
            if (rule.Predicate(exception))
            {
                return rule.Resolver(exception);
            }
        }

        return MapGeneric(exception);
    }

    private static ExceptionMap MapByCode(int code, int statusCode, string message) =>
        new(statusCode, code, message, Failure: CreatePlatformError(code, message));

    private static ExceptionMap MapAdminDomainException(AdminDomainException exception) =>
        new(
            exception.HttpStatus,
            exception.Code,
            exception.Message,
            exception.Errors,
            exception.ErrorMeta,
            CreatePlatformError(exception.Code, exception.Message, exception.Errors, exception.ErrorMeta));

    private static ExceptionMap MapPostgresException(PostgresException ex)
    {
        return ex.SqlState switch
        {
            "23505" => CreateExceptionMap(StatusCodes.Status409Conflict, ApiResultCodes.Conflict, MapPostgresUniqueViolation(ex)),
            "23503" => CreateExceptionMap(StatusCodes.Status409Conflict, ApiResultCodes.Conflict, "关联数据约束冲突，请先处理依赖关系后再操作。"),
            _ => CreateExceptionMap(StatusCodes.Status500InternalServerError, ApiResultCodes.InternalError, "数据库异常，请稍后重试。"),
        };
    }

    private static ExceptionMap MapGeneric(Exception ex) =>
        CreateExceptionMap(StatusCodes.Status500InternalServerError, ApiResultCodes.InternalError, "服务器内部错误，请稍后重试。");

    private static ExceptionMap CreateExceptionMap(int status, int code, string message) =>
        new(status, code, message, Failure: CreatePlatformError(code, message));

    private static HarborError CreatePlatformError(
        int code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null,
        object? metadata = null)
    {
        var definition = code switch
        {
            ApiResultCodes.BadRequest => HarborPlatformErrorCodes.Validation,
            ApiResultCodes.Unauthorized => HarborPlatformErrorCodes.Unauthorized,
            ApiResultCodes.Forbidden => HarborPlatformErrorCodes.Forbidden,
            ApiResultCodes.NotFound => HarborPlatformErrorCodes.NotFound,
            ApiResultCodes.Conflict => HarborPlatformErrorCodes.Conflict,
            ApiResultCodes.InternalError => HarborPlatformErrorCodes.Internal,
            _ => HarborPlatformErrorCodes.Business,
        };
        return definition.Create(fieldErrors: errors, metadata: metadata, defaultMessage: message);
    }

    private static string MapPostgresUniqueViolation(PostgresException ex)
    {
        return ex.ConstraintName switch
        {
            "ux_admin_feature_action" => "已存在同编码动作（按钮）。",
            "ux_admin_feature_action_permission" => "权限编码已存在，请使用不同的权限码。",
            "ux_admin_feature_action_api" => "动作与接口绑定重复，请检查 API 绑定项。",
            "ux_admin_feature_field" => "字段编码重复，请使用不同的字段编码。",
            "ux_admin_feature_api" => "接口编码重复，请使用不同的接口编码。",
            _ => "数据库唯一性约束冲突，请检查输入内容。",
        };
    }

    private sealed record ExceptionMap(
        int StatusCode,
        int Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Errors = null,
        object? Metadata = null,
        HarborError? Failure = null)
    {
        public HarborError Error { get; } = Failure ?? HarborPlatformErrorCodes.Internal.Create();
    }
}

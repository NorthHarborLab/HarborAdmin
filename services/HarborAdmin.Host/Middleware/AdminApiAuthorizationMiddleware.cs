using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Host.Infrastructure.Options;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Host.Middleware;

/// <summary>
/// Admin API 权限拦截中间件（Host 管道组合）。
/// </summary>
public sealed class AdminApiAuthorizationMiddleware(RequestDelegate next, IOptions<AdminHostSecurityOptions> securityOptions)
{
    /// <summary>
    /// Host 安全管道配置。
    /// </summary>
    private readonly AdminHostSecurityOptions _options = securityOptions.Value;

    /// <summary>
    /// 执行权限校验。
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, IAdminApiAccessEvaluator apiAccessEvaluator)
    {
        if (IsAnonymousAllowed(context))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value;

        if (!ShouldAuthenticate(path))
        {
            await next(context);
            return;
        }

        if (currentUser.Id <= 0)
        {
            await WriteFailureAsync(context, StatusCodes.Status401Unauthorized, ApiResultCodes.Unauthorized, "未登录或登录已过期。");
            return;
        }

        if (IsAuthenticatedOnly(context))
        {
            await next(context);
            return;
        }

        var allowed = await apiAccessEvaluator.CanAccessAsync(
            currentUser.Id,
            path ?? string.Empty,
            context.Request.Method,
            context.RequestAborted);

        if (!allowed)
        {
            await WriteFailureAsync(context, StatusCodes.Status403Forbidden, ApiResultCodes.Forbidden, "没有访问该接口的权限。");
            return;
        }

        await next(context);
    }

    /// <summary>
    /// 判断当前 Endpoint 是否允许匿名访问。
    /// </summary>
    private static bool IsAnonymousAllowed(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

    /// <summary>
    /// 判断当前路径是否需要进入 Admin 登录与权限校验。
    /// </summary>
    private bool ShouldAuthenticate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return _options.ProtectedPathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 判断当前 Endpoint 是否只要求登录而跳过权限点绑定校验。
    /// </summary>
    private static bool IsAuthenticatedOnly(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<AuthenticatedOnlyAttribute>() is not null;

    /// <summary>
    /// 写入统一 API 失败响应。
    /// </summary>
    private static async Task WriteFailureAsync(HttpContext context, int statusCode, int code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = JsonSerializer.Serialize(ApiResult.Fail(code, message), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        await context.Response.WriteAsync(payload, context.RequestAborted);
    }
}
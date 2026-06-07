using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.Host.Infrastructure.Options;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Host.Middleware;

/// <summary>
/// Admin API 权限拦截中间件（Host 管道组合）。
/// </summary>
public sealed class AdminApiAuthorizationMiddleware(
    RequestDelegate next,
    IOptions<AdminHostSecurityOptions> securityOptions)
{
    private readonly AdminHostSecurityOptions _options = securityOptions.Value;

    /// <summary>
    /// 执行权限校验。
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        IAdminApiAccessEvaluator apiAccessEvaluator)
    {
        var path = context.Request.Path.Value;
        if (IsPublic(path))
        {
            await next(context);
            return;
        }

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

    private bool IsPublic(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return _options.PublicPathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldAuthenticate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return _options.ProtectedPathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

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

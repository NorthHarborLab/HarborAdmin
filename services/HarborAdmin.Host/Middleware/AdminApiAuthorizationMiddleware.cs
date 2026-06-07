using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.Modules.Admin.Application.Services.Authorization;

namespace HarborAdmin.Host.Middleware;

/// <summary>
/// Admin API 权限拦截中间件（Host 管道组合）。
/// </summary>
public sealed class AdminApiAuthorizationMiddleware(RequestDelegate next)
{
    private static readonly string[] PublicPrefixes =
    [
        "/api/auth/crypto-challenge",
        "/api/auth/captcha",
        "/api/auth/login",
        "/api/auth/refresh",
        "/openapi",
        "/admin/international/resources",
    ];

    /// <summary>
    /// 执行权限校验。
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, ApiAuthorizationService apiAuthorizationService)
    {
        if (IsPublic(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value;
        var shouldAuthenticate = path?.StartsWith("/system", StringComparison.OrdinalIgnoreCase) == true
            || path?.StartsWith("/api/admin/feature-design", StringComparison.OrdinalIgnoreCase) == true
            || path?.StartsWith("/api/admin/features", StringComparison.OrdinalIgnoreCase) == true
            || path?.StartsWith("/api/admin/dynamic-crud", StringComparison.OrdinalIgnoreCase) == true
            || path?.StartsWith("/api/user", StringComparison.OrdinalIgnoreCase) == true
            || path?.StartsWith("/menu", StringComparison.OrdinalIgnoreCase) == true;

        if (!shouldAuthenticate)
        {
            await next(context);
            return;
        }

        if (currentUser.Id <= 0)
        {
            await WriteFailureAsync(context, StatusCodes.Status401Unauthorized, ApiResultCodes.Unauthorized, "未登录或登录已过期。");
            return;
        }

        var allowed = await apiAuthorizationService.CanAccessApiAsync(
            currentUser.Id,
            context.Request.Path.Value ?? string.Empty,
            context.Request.Method,
            context.RequestAborted);

        if (!allowed)
        {
            await WriteFailureAsync(context, StatusCodes.Status403Forbidden, ApiResultCodes.Forbidden, "没有访问该接口的权限。");
            return;
        }

        await next(context);
    }

    private static bool IsPublic(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return PublicPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
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

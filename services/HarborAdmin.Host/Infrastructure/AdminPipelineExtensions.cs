using HarborAdmin.Host.Middleware;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// Admin 模块 HTTP 管道扩展（Host 组合根）。
/// </summary>
public static class AdminPipelineExtensions
{
    /// <summary>
    /// 启用 Admin access token 解析中间件。
    /// </summary>
    public static IApplicationBuilder UseAdminAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<AdminAuthenticationMiddleware>();

    /// <summary>
    /// 启用 Admin API 权限校验中间件。
    /// </summary>
    public static IApplicationBuilder UseAdminApiAuthorization(this IApplicationBuilder app) =>
        app.UseMiddleware<AdminApiAuthorizationMiddleware>();
}

using HarborAdmin.Host.Infrastructure.Security;
using HarborAdmin.Modules.Admin.Application.Abstractions;

namespace HarborAdmin.Host.Middleware;

/// <summary>
/// Admin access token 解析中间件（Host 管道组合）。
/// </summary>
public sealed class AdminAuthenticationMiddleware(RequestDelegate next)
{
    /// <summary>
    /// 解析当前请求用户。
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        AdminRequestUser requestUser,
        IAdminPrincipalResolver principalResolver)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;

        var principal = await principalResolver.ResolveAsync(token, context.RequestAborted);
        if (principal is not null)
        {
            requestUser.Id = principal.Id;
            requestUser.UserName = principal.UserName;
            requestUser.DisplayName = principal.DisplayName;
        }

        await next(context);
    }
}

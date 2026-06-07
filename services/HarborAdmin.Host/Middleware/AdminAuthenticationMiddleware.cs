using HarborAdmin.Host.Infrastructure.Security;
using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Infrastructure.Security;

namespace HarborAdmin.Host.Middleware;

/// <summary>
/// Admin access token 解析中间件（Host 管道组合）。
/// </summary>
public sealed class AdminAuthenticationMiddleware(RequestDelegate next, AdminTokenProtector tokenProtector)
{
    /// <summary>
    /// 解析当前请求用户。
    /// </summary>
    public async Task InvokeAsync(HttpContext context, AdminRequestUser requestUser, UserService userService)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;

        var payload = tokenProtector.ValidateAccessToken(token);
        if (payload is not null)
        {
            var user = await userService.GetUserAsync(payload.UserId, context.RequestAborted);
            if (user is not null && user.Enabled)
            {
                requestUser.Id = user.Id;
                requestUser.UserName = user.UserName;
                requestUser.DisplayName = user.DisplayName;
            }
        }

        await next(context);
    }
}

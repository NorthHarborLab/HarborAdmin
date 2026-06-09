using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Infrastructure.Security;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 基于 Admin access token 的用户解析实现。
/// </summary>
public sealed class AdminPrincipalResolver(AdminTokenProtector tokenProtector, UserService userService) : IAdminPrincipalResolver
{
    /// <inheritdoc />
    public async Task<AdminPrincipalSnapshot?> ResolveAsync(string? accessToken, CancellationToken cancellationToken = default)
    {
        var payload = tokenProtector.ValidateAccessToken(accessToken);
        if (payload is null)
        {
            return null;
        }

        var user = await userService.GetUserAsync(payload.UserId, cancellationToken);
        if (user is null || !user.Enabled)
        {
            return null;
        }

        return new AdminPrincipalSnapshot(user.Id, user.UserName, user.DisplayName);
    }
}

using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Menu;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Application.Services.User;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 用户会话快照服务。
/// </summary>
public sealed class SessionService(
    AdminServiceContext context,
    AccessQueryService accessQuery,
    FieldPolicyService fieldPolicyService,
    UserService userService)
{
    /// <summary>
    /// 构建当前用户的会话访问包，包含用户信息、权限、路由与字段策略。
    /// </summary>
    public async Task<SessionSnapshotDto> BuildSessionAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedDomainException("未登录或登录已过期。");
        }

        var user = await userService.GetUserAsync(userId, cancellationToken)
                   ?? throw new NotFoundDomainException("用户不存在。");
        var roles = await accessQuery.GetEnabledUserRolesAsync(userId, cancellationToken);
        var permissions = await accessQuery.GetUserPermissionsAsync(userId, cancellationToken);
        var menus = await accessQuery.GetUserMenusAsync(userId, cancellationToken);
        var routes = await MenuMapper.BuildRoutesAsync(context.Orm, menus, permissions, cancellationToken);
        var fieldPolicies = await fieldPolicyService.GetFieldPoliciesForUserAsync(userId, cancellationToken);
        var dataScopes = await accessQuery.GetDataScopesAsync(roles, cancellationToken);
        var sessionVersion = await context.GetSessionVersionValueAsync(cancellationToken);
        var homePath = user.HomePath;
        if (string.IsNullOrWhiteSpace(homePath) || menus.All(menu => menu.RoutePath != homePath))
        {
            homePath = menus.FirstOrDefault(menu => menu.MenuType != "catalog")?.RoutePath ?? "/dashboard";
        }

        return new SessionSnapshotDto(
            sessionVersion,
            new CurrentUserDto(
                user.Id.ToString(),
                user.UserName,
                user.DisplayName,
                user.Avatar ?? string.Empty,
                user.Remark ?? string.Empty,
                homePath,
                roles.Select(role => role.RoleCode).ToArray(),
                user.IsSuperAdmin),
            permissions,
            routes,
            fieldPolicies,
            dataScopes,
            homePath);
    }

    /// <summary>
    /// 获取全局 sessionVersion，供前端判断权限/菜单是否需要刷新。
    /// </summary>
    public async Task<SessionVersionDto> GetSessionVersionAsync(CancellationToken cancellationToken) =>
        new(await context.GetSessionVersionValueAsync(cancellationToken));
}

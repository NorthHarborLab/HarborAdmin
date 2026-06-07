using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 用户角色、权限、菜单与数据范围查询服务。
/// </summary>
public sealed class AccessQueryService(AccessCacheService accessCache)
{
    /// <summary>
    /// 判断用户是否为超级管理员。
    /// </summary>
    public async Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        return snapshot.IsSuperAdmin;
    }

    /// <summary>
    /// 获取用户已启用的角色列表。
    /// </summary>
    public async Task<IReadOnlyList<AdminRole>> GetEnabledUserRolesAsync(long userId, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        return snapshot.Roles.Select(AccessCacheService.ToAdminRole).ToArray();
    }

    /// <summary>
    /// 获取用户权限码集合；超级管理员返回全部已启用权限。
    /// </summary>
    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        return snapshot.Permissions;
    }

    /// <summary>
    /// 获取用户可访问的菜单列表（不含按钮类型）。
    /// </summary>
    public async Task<IReadOnlyList<AdminMenu>> GetUserMenusAsync(long userId, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        return snapshot.Menus.Select(AccessCacheService.ToAdminMenu).ToArray();
    }

    /// <summary>
    /// 获取角色数据范围配置。
    /// </summary>
    public async Task<IReadOnlyList<DataScopeDto>> GetDataScopesAsync(IReadOnlyList<AdminRole> roles, CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return [];
        }

        var scopes = new List<DataScopeDto>();
        foreach (var role in roles)
        {
            var roleScopes = await accessCache.GetRoleDataScopesAsync(role.Id, cancellationToken);
            scopes.AddRange(roleScopes.Select(scope => new DataScopeDto(
                role.RoleCode,
                scope.ScopeType,
                scope.ScopeValueType,
                scope.ScopeValueId?.ToString())));
        }

        return scopes;
    }

    /// <summary>
    /// 根据角色数据范围计算用户可访问的部门 ID 集合。
    /// </summary>
    public async Task<ISet<long>?> GetAllowedDepartmentIdsAsync(long userId, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        return snapshot.AllowedDepartmentIds switch
        {
            null => null,
            { Length: 0 } => new HashSet<long>(),
            var ids => ids.ToHashSet(),
        };
    }

    /// <summary>
    /// 判断请求路径是否匹配 API 模板（支持 <c>{param}</c> 占位符）。
    /// </summary>
    public static bool PathMatches(string template, string path) =>
        AccessPathMatcher.Matches(template, path);
}

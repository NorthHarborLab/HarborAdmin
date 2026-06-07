using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Shared;

/// <summary>
/// 用户角色、权限、菜单与数据范围查询服务。
/// </summary>
public sealed class AccessQueryService(AdminServiceContext context)
{
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 获取用户已启用的角色列表。
    /// </summary>
    public async Task<IReadOnlyList<AdminRole>> GetEnabledUserRolesAsync(long userId, CancellationToken cancellationToken)
    {
        var userRoles = await Orm.Select<AdminUserRole>().Where(link => link.UserId == userId).ToListAsync(cancellationToken);
        if (userRoles.Count == 0)
        {
            return [];
        }

        var roleIds = userRoles.Select(link => link.RoleId).ToArray();
        return await Orm.Select<AdminRole>()
            .Where(role => role.Enabled && roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户权限码集合；超级管理员返回全部已启用权限。
    /// </summary>
    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken)
    {
        var roles = await GetEnabledUserRolesAsync(userId, cancellationToken);
        if (roles.Any(role => role.RoleCode == "super_admin"))
        {
            var allPermissions = await Orm.Select<AdminFeatureAction>().Where(action => action.Enabled).ToListAsync(cancellationToken);
            return allPermissions.Select(action => action.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var roleIds = roles.Select(role => role.Id).ToArray();
        if (roleIds.Length == 0)
        {
            return [];
        }

        var rolePermissions = await Orm.Select<AdminRolePermission>()
            .Where(link => roleIds.Contains(link.RoleId))
            .ToListAsync(cancellationToken);
        return rolePermissions.Select(link => link.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// 获取用户可访问的菜单列表（不含按钮类型）。
    /// </summary>
    public async Task<IReadOnlyList<AdminMenu>> GetUserMenusAsync(long userId, CancellationToken cancellationToken)
    {
        var roles = await GetEnabledUserRolesAsync(userId, cancellationToken);
        var query = Orm.Select<AdminMenu>().Where(menu => menu.Enabled && menu.MenuType != "button");
        if (roles.Any(role => role.RoleCode == "super_admin"))
        {
            return await query.OrderBy(menu => menu.SortOrder).ToListAsync(cancellationToken);
        }

        var roleIds = roles.Select(role => role.Id).ToArray();
        if (roleIds.Length == 0)
        {
            return [];
        }

        var roleMenus = await Orm.Select<AdminRoleMenu>().Where(link => roleIds.Contains(link.RoleId)).ToListAsync(cancellationToken);
        var menuIds = roleMenus.Select(link => link.MenuId).Distinct().ToArray();
        return await query.Where(menu => menuIds.Contains(menu.Id)).OrderBy(menu => menu.SortOrder).ToListAsync(cancellationToken);
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

        var roleIds = roles.Select(role => role.Id).ToArray();
        var scopes = await Orm.Select<AdminRoleDataScope>().Where(scope => roleIds.Contains(scope.RoleId)).ToListAsync(cancellationToken);
        return scopes.Select(scope =>
        {
            var role = roles.First(item => item.Id == scope.RoleId);
            return new DataScopeDto(role.RoleCode, scope.ScopeType, scope.ScopeValueType, scope.ScopeValueId?.ToString());
        }).ToArray();
    }

    /// <summary>
    /// 根据角色数据范围计算用户可访问的部门 ID 集合。
    /// </summary>
    public async Task<ISet<long>?> GetAllowedDepartmentIdsAsync(long userId, CancellationToken cancellationToken)
    {
        var roles = await GetEnabledUserRolesAsync(userId, cancellationToken);
        if (roles.Any(role => role.DataScopeType == "All"))
        {
            return null;
        }

        var user = await Orm.Select<AdminUser>().Where(item => item.Id == userId).ToOneAsync(cancellationToken);
        if (user?.DeptId is null)
        {
            return new HashSet<long>();
        }

        if (roles.Any(role => role.DataScopeType is "DeptWithChildren" or "SelfWithSubordinates"))
        {
            var departments = await Orm.Select<AdminDepartment>().ToListAsync(cancellationToken);
            var ids = new HashSet<long> { user.DeptId.Value };
            AddChildDepartmentIds(user.DeptId.Value, departments, ids);
            return ids;
        }

        if (roles.Any(role => role.DataScopeType == "Dept"))
        {
            return new HashSet<long> { user.DeptId.Value };
        }

        return new HashSet<long>();
    }

    /// <summary>
    /// 判断请求路径是否匹配 API 模板（支持 <c>{param}</c> 占位符）。
    /// </summary>
    public static bool PathMatches(string template, string path)
    {
        var templateParts = template.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathParts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (templateParts.Length != pathParts.Length)
        {
            return false;
        }

        for (var i = 0; i < templateParts.Length; i++)
        {
            if (templateParts[i].StartsWith('{') && templateParts[i].EndsWith('}'))
            {
                continue;
            }

            if (!string.Equals(templateParts[i], pathParts[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static void AddChildDepartmentIds(long parentId, IReadOnlyList<AdminDepartment> departments, ISet<long> ids)
    {
        foreach (var child in departments.Where(dept => dept.ParentId == parentId))
        {
            if (ids.Add(child.Id))
            {
                AddChildDepartmentIds(child.Id, departments, ids);
            }
        }
    }
}

using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 系统管理聚合 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public async Task<AdminUser?> GetUserAggregateAsync(long userId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .Where(user => user.Id == userId)
            .Include(user => user.Dept)
            .IncludeMany(user => user.UserRoles, then => then.Include(link => link.Role))
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUser>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .OrderBy(user => user.Id)
            .IncludeMany(user => user.UserRoles, then => then.Include(link => link.Role))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminRole?> GetRoleAggregateAsync(long roleId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRole>()
            .Where(role => role.Id == roleId)
            .IncludeMany(role => role.RoleMenus, then => then.Include(link => link.Menu))
            .IncludeMany(role => role.RolePermissions, then => then.Include(link => link.AdminFeatureAction))
            .IncludeMany(role => role.FieldPermissions, then => then.Include(link => link.AdminFeatureField))
            .IncludeMany(role => role.DataScopes)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRole>> ListRolesWithGrantsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRole>()
            .OrderBy(role => role.Id)
            .IncludeMany(role => role.RoleMenus)
            .IncludeMany(role => role.RolePermissions)
            .IncludeMany(role => role.FieldPermissions)
            .IncludeMany(role => role.DataScopes)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminMenu?> GetMenuWithFeatureAsync(long menuId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminMenu>()
            .Where(menu => menu.Id == menuId)
            .Include(menu => menu.AdminFeature)
            .Include(menu => menu.Parent)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminMenu>> ListMenusWithFeaturesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminMenu>()
            .OrderBy(menu => menu.SortOrder)
            .Include(menu => menu.AdminFeature)
            .ToListAsync(cancellationToken);
}

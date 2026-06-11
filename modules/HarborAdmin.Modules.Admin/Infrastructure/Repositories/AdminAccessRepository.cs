using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 访问控制 FreeSql 仓储。
/// </summary>
public sealed class AdminAccessRepository(IAdminDbContext db)
    : FreeSqlModuleRepository<IAdminDbContext>(db), IAdminAccessRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUserRole>> GetUserRoleLinksAsync(long userId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUserRole>()
            .Where(link => link.UserId == userId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRole>> GetRolesByIdsAsync(IReadOnlyList<long> roleIds, bool enabledOnly, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminRole>().Where(role => roleIds.Contains(role.Id));
        if (enabledOnly)
        {
            query = query.Where(role => role.Enabled);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeatureAction>> GetEnabledFeatureActionsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureAction>()
            .Where(action => action.Enabled
                             && action.AdminFeature.NodeType != AdminFeatureNodeType.Category
                             && action.AdminFeature.Enabled)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRolePermission>> GetRolePermissionLinksAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRolePermission>()
            .Where(link => roleIds.Contains(link.RoleId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleMenu>> GetRoleMenuLinksAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRoleMenu>()
            .Where(link => roleIds.Contains(link.RoleId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleDataScope>> GetRoleDataScopesAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRoleDataScope>()
            .Where(scope => roleIds.Contains(scope.RoleId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeatureApi>> GetEnabledFeatureApisAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureApi>()
            .Where(api => api.Enabled
                          && api.AdminFeature.NodeType != AdminFeatureNodeType.Category
                          && api.AdminFeature.Enabled)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeatureActionApi>> GetFeatureActionApiLinksAsync(long featureApiId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureActionApi>()
            .Where(link => link.AdminFeatureApiId == featureApiId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleFieldPermission>> GetRoleFieldPoliciesAsync(long roleId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminRoleFieldPermission>()
            .Where(policy => policy.RoleId == roleId)
            .Include(policy => policy.AdminFeatureField)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminUser?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminUser>()
            .Where(item => item.Id == userId)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminMenu>> ListEnabledMenusAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminMenu>()
            .Where(menu => menu.Enabled && menu.MenuType != "button")
            .OrderBy(menu => menu.SortOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminMenu>> ListEnabledMenusByIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminMenu>()
            .Where(menu => menuIds.Contains(menu.Id) && menu.Enabled && menu.MenuType != "button")
            .OrderBy(menu => menu.SortOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminDepartment>> ListDepartmentsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminDepartment>().ToListAsync(cancellationToken);
}

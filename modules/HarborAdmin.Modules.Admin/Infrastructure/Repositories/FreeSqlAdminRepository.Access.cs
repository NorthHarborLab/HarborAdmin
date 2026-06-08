using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 访问控制链接表 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
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
            .Where(action => action.Enabled)
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
            .Where(api => api.Enabled)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeatureActionApi>> GetFeatureActionApiLinksAsync(long featureApiId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureActionApi>()
            .Where(link => link.AdminFeatureApiId == featureApiId)
            .ToListAsync(cancellationToken);
}

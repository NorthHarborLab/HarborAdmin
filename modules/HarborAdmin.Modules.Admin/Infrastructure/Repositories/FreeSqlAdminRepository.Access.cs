using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 访问控制链接表 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AdminUserRole>> GetUserRoleLinksAsync(long userId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminUserRole>()
            .Where(link => link.UserId == userId)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminUserRole>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminRole>> GetRolesByIdsAsync(IReadOnlyList<long> roleIds, bool enabledOnly, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminRole>().Where(role => roleIds.Contains(role.Id));
        if (enabledOnly)
        {
            query = query.Where(role => role.Enabled);
        }

        return query.ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminRole>)task.Result, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminFeatureAction>> GetEnabledFeatureActionsAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminFeatureAction>()
            .Where(action => action.Enabled)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminFeatureAction>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminRolePermission>> GetRolePermissionLinksAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminRolePermission>()
            .Where(link => roleIds.Contains(link.RoleId))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminRolePermission>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminRoleMenu>> GetRoleMenuLinksAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminRoleMenu>()
            .Where(link => roleIds.Contains(link.RoleId))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminRoleMenu>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminRoleDataScope>> GetRoleDataScopesAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminRoleDataScope>()
            .Where(scope => roleIds.Contains(scope.RoleId))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminRoleDataScope>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminFeatureApi>> GetEnabledFeatureApisAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminFeatureApi>()
            .Where(api => api.Enabled)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminFeatureApi>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminFeatureActionApi>> GetFeatureActionApiLinksAsync(long featureApiId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminFeatureActionApi>()
            .Where(link => link.AdminFeatureApiId == featureApiId)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminFeatureActionApi>)task.Result, cancellationToken);
}

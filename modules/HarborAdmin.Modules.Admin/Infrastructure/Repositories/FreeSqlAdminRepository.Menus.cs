using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 菜单扩展 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AdminFeature>> ListFeaturesAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminFeature>()
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminFeature>)task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminFeatureAction>> ListEnabledFeatureActionsAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminFeatureAction>()
            .Where(action => action.Enabled)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<AdminFeatureAction>)task.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminMenu>> ListSiblingMenusAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminMenu>();
        query = parentId.HasValue
            ? query.Where(menu => menu.ParentId == parentId.Value)
            : query.Where(menu => !menu.ParentId.HasValue);
        return await query.OrderBy(menu => menu.SortOrder).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> CountChildMenusAsync(long parentId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminMenu>().Where(menu => menu.ParentId == parentId).CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> MenuNameExistsAsync(string name, long? id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminMenu>()
            .Where(menu => menu.Title == name && (!id.HasValue || menu.Id != id.Value))
            .AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> MenuPathExistsAsync(string path, long? id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminMenu>()
            .Where(menu => menu.RoutePath == path && (!id.HasValue || menu.Id != id.Value))
            .AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateMenusAsync(IReadOnlyList<AdminMenu> menus, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminMenu>().SetSource(menus).ExecuteAffrowsAsync(cancellationToken);
}

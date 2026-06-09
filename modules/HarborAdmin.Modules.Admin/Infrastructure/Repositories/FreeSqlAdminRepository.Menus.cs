using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 菜单扩展 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeature>> ListFeaturesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(feature => feature.NodeType != AdminFeatureNodeType.Category)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeatureAction>> ListEnabledFeatureActionsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureAction>()
            .Where(action => action.Enabled
                             && action.AdminFeature.NodeType != AdminFeatureNodeType.Category
                             && action.AdminFeature.Enabled)
            .ToListAsync(cancellationToken);

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
    public async Task<IReadOnlyList<AdminMenu>> GetMenusByIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminMenu>()
            .Where(menu => menuIds.Contains(menu.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<long> CountChildMenusAsync(long parentId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminMenu>().Where(menu => menu.ParentId == parentId).CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> MenuNameExistsAsync(string name, long? id, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminMenu>().Where(menu => menu.Title == name);
        if (id.HasValue)
        {
            query = query.Where(menu => menu.Id != id.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> MenuPathExistsAsync(string path, long? id, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminMenu>().Where(menu => menu.RoutePath == path);
        if (id.HasValue)
        {
            query = query.Where(menu => menu.Id != id.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateMenusAsync(IReadOnlyList<AdminMenu> menus, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminMenu>().SetSource(menus).ExecuteAffrowsAsync(cancellationToken);
}

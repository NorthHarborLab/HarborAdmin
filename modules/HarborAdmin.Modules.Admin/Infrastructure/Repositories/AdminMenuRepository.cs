using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 菜单 FreeSql 仓储。
/// </summary>
public sealed class AdminMenuRepository(IAdminDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAdminDbContext>(db, unitOfWorkManager), IAdminMenuRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeature>> ListFeaturesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(feature => feature.NodeType != AdminFeatureNodeType.Category)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeature>> ListEnabledFeaturesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(feature => feature.NodeType != AdminFeatureNodeType.Category && feature.Enabled)
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
    public async Task<AdminFeature?> ResolveFeatureByCodeAsync(string? featureCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return null;
        }

        return await FreeSql.Select<AdminFeature>()
            .Where(feature => feature.FeatureCode == featureCode.Trim()
                              && feature.NodeType != AdminFeatureNodeType.Category)
            .ToOneAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveMenuAsync(AdminMenu menu, bool isUpdate, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository<AdminMenu>(cascadeSave: true);
        if (isUpdate)
        {
            await repository.UpdateAsync(menu, cancellationToken);
        }
        else
        {
            await repository.InsertAsync(menu, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task DeleteMenuCascadeAsync(long menuId, CancellationToken cancellationToken = default) =>
        GetRepository<AdminMenu>(cascadeSave: true).DeleteCascadeByDatabaseAsync(menu => menu.Id == menuId, cancellationToken);

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

using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 功能设计 FreeSql 仓储。
/// </summary>
public sealed class AdminFeatureDesignRepository(IAdminDbContext db)
    : FreeSqlModuleRepository<IAdminDbContext>(db), IAdminFeatureDesignRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeature>> ListFeatureTreeNodesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .OrderBy(item => item.ParentId)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.FeatureCode)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeature?> GetFeatureAggregateAsync(string featureCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.FeatureCode == featureCode)
            .IncludeMany(item => item.Fields)
            .IncludeMany(item => item.Apis)
            .IncludeMany(item => item.Actions, then => then.IncludeMany(action => action.ActionApis))
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeature?> GetEnabledFeatureRuntimeAsync(string featureCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.FeatureCode == featureCode
                           && item.Enabled
                           && item.NodeType != AdminFeatureNodeType.Category)
            .IncludeMany(item => item.Fields)
            .IncludeMany(item => item.Apis)
            .IncludeMany(item => item.Actions, then => then.IncludeMany(action => action.ActionApis))
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeature?> GetFeatureByCodeAsync(string featureCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.FeatureCode == featureCode)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeature?> GetFeatureByIdAsync(long featureId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.Id == featureId)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> FeatureCodeExistsAsync(string featureCode, long? excludeFeatureId = null, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminFeature>().Where(item => item.FeatureCode == featureCode);
        if (excludeFeatureId.HasValue)
        {
            query = query.Where(item => item.Id != excludeFeatureId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveFeatureAsync(AdminFeature feature, bool isUpdate, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository<AdminFeature>(cascadeSave: true);
        if (isUpdate)
        {
            await repository.UpdateAsync(feature, cancellationToken);
        }
        else
        {
            await repository.InsertAsync(feature, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task UpdateFeaturesAsync(IReadOnlyList<AdminFeature> features, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminFeature>().SetSource(features).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public void SaveFeatureChildren(AdminFeature feature, string propertyName) =>
        GetRepository<AdminFeature>(cascadeSave: true).SaveMany(feature, propertyName);

    /// <inheritdoc />
    public void SaveActionChildren(AdminFeatureAction action, string propertyName) =>
        GetRepository<AdminFeatureAction>(cascadeSave: true).SaveMany(action, propertyName);

    /// <inheritdoc />
    public async Task IncrementFeatureSchemaVersionAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        var feature = await FreeSql.Select<AdminFeature>()
                          .Where(item => item.FeatureCode == featureCode
                                         && item.NodeType != AdminFeatureNodeType.Category)
                          .ToOneAsync(cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found.");
        feature.SchemaVersion++;
        feature.UpdatedAt = DateTimeOffset.UtcNow;
        await FreeSql.Update<AdminFeature>().SetSource(feature).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> CountFeatureChildrenAsync(long featureId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminFeature>().Where(item => item.ParentId == featureId).CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsFeatureUsedByMenuAsync(long featureId, string featureCode, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AdminMenu>()
            .Where(menu => menu.AdminFeatureId == featureId || menu.FeatureCode == featureCode)
            .AnyAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteFeatureCascadeAsync(long featureId, CancellationToken cancellationToken = default) =>
        GetRepository<AdminFeature>(cascadeSave: true).DeleteCascadeByDatabaseAsync(item => item.Id == featureId, cancellationToken);

    /// <inheritdoc />
    public Task DeleteRolePermissionLinksByActionIdsAsync(IReadOnlyList<long> actionIds, CancellationToken cancellationToken = default) =>
        actionIds.Count == 0
            ? Task.CompletedTask
            : FreeSql.Delete<AdminRolePermission>().Where(item => actionIds.Contains(item.AdminFeatureActionId)).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteRoleFieldPermissionLinksByFieldIdsAsync(IReadOnlyList<long> fieldIds, CancellationToken cancellationToken = default) =>
        fieldIds.Count == 0
            ? Task.CompletedTask
            : FreeSql.Delete<AdminRoleFieldPermission>().Where(item => fieldIds.Contains(item.AdminFeatureFieldId)).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteRoleFieldPermissionLinksByFieldIdAsync(long fieldId, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AdminRoleFieldPermission>()
            .Where(item => item.AdminFeatureFieldId == fieldId)
            .ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<AdminFeature>> ListSortableFeatureSiblingsAsync(long? parentId, AdminFeatureNodeType nodeType, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminFeature>();
        if (parentId.HasValue)
        {
            query = query.Where(item => item.ParentId == parentId.Value);
        }
        else if (nodeType == AdminFeatureNodeType.Category)
        {
            query = query.Where(item => item.ParentId == null && item.NodeType == AdminFeatureNodeType.Category);
        }
        else
        {
            query = query.Where(item => item.ParentId == null && item.NodeType != AdminFeatureNodeType.Category);
        }

        return await query
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.FeatureCode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateFeatureFieldsAsync(IReadOnlyList<AdminFeatureField> fields, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminFeatureField>().SetSource(fields).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeature>> ListFeatureApiTreeAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.NodeType == AdminFeatureNodeType.Feature)
            .IncludeMany(item => item.Apis)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.FeatureCode)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateFeatureApisAsync(IReadOnlyList<AdminFeatureApi> apis, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminFeatureApi>().SetSource(apis).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeatureAction?> GetFeatureActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureAction>()
            .Where(item => item.FeatureCode == featureCode && item.ActionCode == actionCode)
            .IncludeMany(item => item.ActionApis)
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> PermissionCodeExistsAsync(string permissionCode, long? excludeActionId = null, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminFeatureAction>().Where(item => item.PermissionCode == permissionCode);
        if (excludeActionId.HasValue)
        {
            query = query.Where(item => item.Id != excludeActionId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateRolePermissionCodeAsync(long actionId, string permissionCode, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminRolePermission>()
            .Set(item => item.PermissionCode, permissionCode)
            .Where(item => item.AdminFeatureActionId == actionId)
            .ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateFeatureActionsAsync(IReadOnlyList<AdminFeatureAction> actions, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AdminFeatureAction>().SetSource(actions).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteRolePermissionLinksByActionIdAsync(long actionId, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AdminRolePermission>()
            .Where(item => item.AdminFeatureActionId == actionId)
            .ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteActionApiLinksAsync(long actionId, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AdminFeatureActionApi>()
            .Where(item => item.AdminFeatureActionId == actionId)
            .ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminFeatureApi>> GetFeatureApisByIdsAsync(IReadOnlyList<long> apiIds, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureApi>()
            .Where(item => apiIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertActionApiLinksAsync(IReadOnlyList<AdminFeatureActionApi> links, CancellationToken cancellationToken = default) =>
        links.Count == 0
            ? Task.CompletedTask
            : FreeSql.Insert(links).ExecuteAffrowsAsync(cancellationToken);
}

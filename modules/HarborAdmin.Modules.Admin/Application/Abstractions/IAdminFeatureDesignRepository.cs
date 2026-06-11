using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 功能设计仓储。
/// </summary>
public interface IAdminFeatureDesignRepository
{
    /// <summary>
    /// 加载 Feature 树节点。
    /// </summary>
    Task<IReadOnlyList<AdminFeature>> ListFeatureTreeNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载 Feature 设计态聚合（含字段、接口、动作）。
    /// </summary>
    Task<AdminFeature?> GetFeatureAggregateAsync(string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载已启用的 Feature 运行时聚合。
    /// </summary>
    Task<AdminFeature?> GetEnabledFeatureRuntimeAsync(string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按编码加载 Feature。
    /// </summary>
    Task<AdminFeature?> GetFeatureByCodeAsync(string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 加载 Feature。
    /// </summary>
    Task<AdminFeature?> GetFeatureByIdAsync(long featureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断 Feature 编码是否存在。
    /// </summary>
    Task<bool> FeatureCodeExistsAsync(string featureCode, long? excludeFeatureId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存 Feature。
    /// </summary>
    Task SaveFeatureAsync(AdminFeature feature, bool isUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新 Feature。
    /// </summary>
    Task UpdateFeaturesAsync(IReadOnlyList<AdminFeature> features, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存 Feature 子集合。
    /// </summary>
    void SaveFeatureChildren(AdminFeature feature, string propertyName);

    /// <summary>
    /// 保存动作子集合。
    /// </summary>
    void SaveActionChildren(AdminFeatureAction action, string propertyName);

    /// <summary>
    /// 递增 Feature schemaVersion。
    /// </summary>
    Task IncrementFeatureSchemaVersionAsync(string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计 Feature 子节点。
    /// </summary>
    Task<long> CountFeatureChildrenAsync(long featureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断 Feature 是否已被菜单引用。
    /// </summary>
    Task<bool> IsFeatureUsedByMenuAsync(long featureId, string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 级联删除 Feature。
    /// </summary>
    Task DeleteFeatureCascadeAsync(long featureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除动作关联的角色权限。
    /// </summary>
    Task DeleteRolePermissionLinksByActionIdsAsync(IReadOnlyList<long> actionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字段关联的角色字段权限。
    /// </summary>
    Task DeleteRoleFieldPermissionLinksByFieldIdsAsync(IReadOnlyList<long> fieldIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除单个字段关联的角色字段权限。
    /// </summary>
    Task DeleteRoleFieldPermissionLinksByFieldIdAsync(long fieldId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载同组可排序 Feature。
    /// </summary>
    Task<List<AdminFeature>> ListSortableFeatureSiblingsAsync(long? parentId, AdminFeatureNodeType nodeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新字段。
    /// </summary>
    Task UpdateFeatureFieldsAsync(IReadOnlyList<AdminFeatureField> fields, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载 Feature API 树。
    /// </summary>
    Task<IReadOnlyList<AdminFeature>> ListFeatureApiTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新 API。
    /// </summary>
    Task UpdateFeatureApisAsync(IReadOnlyList<AdminFeatureApi> apis, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载指定 Feature 的动作及其 API 绑定。
    /// </summary>
    Task<AdminFeatureAction?> GetFeatureActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断权限编码是否存在。
    /// </summary>
    Task<bool> PermissionCodeExistsAsync(string permissionCode, long? excludeActionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限表中的权限编码。
    /// </summary>
    Task UpdateRolePermissionCodeAsync(long actionId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新动作。
    /// </summary>
    Task UpdateFeatureActionsAsync(IReadOnlyList<AdminFeatureAction> actions, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除动作关联的角色权限。
    /// </summary>
    Task DeleteRolePermissionLinksByActionIdAsync(long actionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除动作 API 绑定。
    /// </summary>
    Task DeleteActionApiLinksAsync(long actionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 加载 Feature API。
    /// </summary>
    Task<IReadOnlyList<AdminFeatureApi>> GetFeatureApisByIdsAsync(IReadOnlyList<long> apiIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增动作 API 绑定。
    /// </summary>
    Task InsertActionApiLinksAsync(IReadOnlyList<AdminFeatureActionApi> links, CancellationToken cancellationToken = default);
}

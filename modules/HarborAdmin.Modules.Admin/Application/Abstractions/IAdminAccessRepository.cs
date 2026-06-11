using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 访问控制仓储。
/// </summary>
public interface IAdminAccessRepository
{
    /// <summary>
    /// 获取用户角色关联。
    /// </summary>
    Task<IReadOnlyList<AdminUserRole>> GetUserRoleLinksAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 获取角色列表。
    /// </summary>
    Task<IReadOnlyList<AdminRole>> GetRolesByIdsAsync(IReadOnlyList<long> roleIds, bool enabledOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部已启用权限动作。
    /// </summary>
    Task<IReadOnlyList<AdminFeatureAction>> GetEnabledFeatureActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色权限码关联。
    /// </summary>
    Task<IReadOnlyList<AdminRolePermission>> GetRolePermissionLinksAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色菜单关联。
    /// </summary>
    Task<IReadOnlyList<AdminRoleMenu>> GetRoleMenuLinksAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色数据范围配置。
    /// </summary>
    Task<IReadOnlyList<AdminRoleDataScope>> GetRoleDataScopesAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已启用的 Feature API 列表。
    /// </summary>
    Task<IReadOnlyList<AdminFeatureApi>> GetEnabledFeatureApisAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 Feature API 与动作的绑定关系。
    /// </summary>
    Task<IReadOnlyList<AdminFeatureActionApi>> GetFeatureActionApiLinksAsync(long featureApiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色字段策略。
    /// </summary>
    Task<IReadOnlyList<AdminRoleFieldPermission>> GetRoleFieldPoliciesAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按用户 ID 获取用户。
    /// </summary>
    Task<AdminUser?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载已启用菜单。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> ListEnabledMenusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 加载已启用菜单。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> ListEnabledMenusByIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载全部部门。
    /// </summary>
    Task<IReadOnlyList<AdminDepartment>> ListDepartmentsAsync(CancellationToken cancellationToken = default);
}

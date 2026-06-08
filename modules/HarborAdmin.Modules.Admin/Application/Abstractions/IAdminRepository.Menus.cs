using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 菜单扩展仓储。
/// </summary>
public partial interface IAdminRepository
{
    /// <summary>
    /// 加载全部功能资源。
    /// </summary>
    Task<IReadOnlyList<AdminFeature>> ListFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载已启用的功能动作。
    /// </summary>
    Task<IReadOnlyList<AdminFeatureAction>> ListEnabledFeatureActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载同级菜单。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> ListSiblingMenusAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 批量加载菜单。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> GetMenusByIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计子菜单数量。
    /// </summary>
    Task<long> CountChildMenusAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断菜单名称是否已存在。
    /// </summary>
    Task<bool> MenuNameExistsAsync(string name, long? id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断菜单路径是否已存在。
    /// </summary>
    Task<bool> MenuPathExistsAsync(string path, long? id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新菜单。
    /// </summary>
    Task UpdateMenusAsync(IReadOnlyList<AdminMenu> menus, CancellationToken cancellationToken = default);
}

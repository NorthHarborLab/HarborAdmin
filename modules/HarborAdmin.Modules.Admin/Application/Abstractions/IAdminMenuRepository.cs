using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 菜单仓储。
/// </summary>
public interface IAdminMenuRepository
{
    /// <summary>
    /// 加载全部功能资源。
    /// </summary>
    Task<IReadOnlyList<AdminFeature>> ListFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载全部已启用功能资源。
    /// </summary>
    Task<IReadOnlyList<AdminFeature>> ListEnabledFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载指定菜单（含绑定功能）。
    /// </summary>
    Task<AdminMenu?> GetMenuWithFeatureAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载全部菜单（含绑定功能）。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> ListMenusWithFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载同级菜单。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> ListSiblingMenusAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 批量加载菜单。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> GetMenusByIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按功能编码解析功能。
    /// </summary>
    Task<AdminFeature?> ResolveFeatureByCodeAsync(string? featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存菜单。
    /// </summary>
    Task SaveMenuAsync(AdminMenu menu, bool isUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 级联删除菜单。
    /// </summary>
    Task DeleteMenuCascadeAsync(long menuId, CancellationToken cancellationToken = default);

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

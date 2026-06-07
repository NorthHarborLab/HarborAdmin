using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 系统管理聚合仓储。
/// </summary>
public partial interface IAdminRepository
{
    /// <summary>
    /// 加载用户聚合（部门 + 角色）。
    /// </summary>
    Task<AdminUser?> GetUserAggregateAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载用户列表（含角色关系）。
    /// </summary>
    Task<IReadOnlyList<AdminUser>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载角色聚合（含全部授权子集合）。
    /// </summary>
    Task<AdminRole?> GetRoleAggregateAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载角色列表（含全部授权子集合）。
    /// </summary>
    Task<IReadOnlyList<AdminRole>> ListRolesWithGrantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载菜单（含绑定功能）。
    /// </summary>
    Task<AdminMenu?> GetMenuWithFeatureAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载全部菜单（含绑定功能）。
    /// </summary>
    Task<IReadOnlyList<AdminMenu>> ListMenusWithFeaturesAsync(CancellationToken cancellationToken = default);
}

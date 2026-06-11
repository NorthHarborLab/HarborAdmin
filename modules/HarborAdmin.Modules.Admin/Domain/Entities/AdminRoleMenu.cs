using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 角色菜单关系。
/// </summary>
[Index("ux_admin_role_menu", $"{nameof(RoleId)},{nameof(MenuId)}", true)]
public sealed class AdminRoleMenu : EntityBase
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 角色。
    /// </summary>
    [Navigate(nameof(RoleId))]
    public AdminRole Role { get; set; } = null!;

    /// <summary>
    /// 菜单 ID。
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// 菜单。
    /// </summary>
    [Navigate(nameof(MenuId))]
    public AdminMenu Menu { get; set; } = null!;
}

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 菜单和路由入口。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_menu_code", nameof(MenuCode), true)]
[Index("ux_admin_menu_route_path", nameof(RoutePath), true)]
[Index("idx_admin_menu_feature_id", nameof(AdminFeatureId), false)]
public sealed class AdminMenu : AuditableEntity
{
    /// <summary>
    /// 菜单编码。
    /// </summary>
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 上级菜单 ID。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 上级菜单。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public AdminMenu? Parent { get; set; }

    /// <summary>
    /// 子菜单。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<AdminMenu> Children { get; set; } = [];

    /// <summary>
    /// 绑定功能 ID。
    /// </summary>
    public long? AdminFeatureId { get; set; }

    /// <summary>
    /// 绑定功能。
    /// </summary>
    [Navigate(nameof(AdminFeatureId))]
    public AdminFeature? AdminFeature { get; set; }

    /// <summary>
    /// 绑定功能编码（冗余）。
    /// </summary>
    public string? FeatureCode { get; set; }

    /// <summary>
    /// 菜单权限编码。
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 路由路径。
    /// </summary>
    public string RoutePath { get; set; } = string.Empty;

    /// <summary>
    /// 路由名称。
    /// </summary>
    public string RouteName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 菜单图标。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 激活菜单图标。
    /// </summary>
    public string? ActiveIcon { get; set; }

    /// <summary>
    /// 激活菜单路径。
    /// </summary>
    public string? ActivePath { get; set; }

    /// <summary>
    /// 菜单类型。
    /// </summary>
    public string MenuType { get; set; } = "menu";

    /// <summary>
    /// 排序值。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否可见。
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 是否固定标签页。
    /// </summary>
    public bool AffixTab { get; set; }

    /// <summary>
    /// 固定标签页排序。
    /// </summary>
    public int? AffixTabOrder { get; set; }

    /// <summary>
    /// 是否在标签页中隐藏。
    /// </summary>
    public bool HideInTab { get; set; }

    /// <summary>
    /// 是否缓存页面。
    /// </summary>
    public bool KeepAlive { get; set; }

    /// <summary>
    /// 是否在菜单中隐藏子级。
    /// </summary>
    public bool HideChildrenInMenu { get; set; }

    /// <summary>
    /// 外链地址。
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// iframe 地址。
    /// </summary>
    public string? IframeSrc { get; set; }

    /// <summary>
    /// 是否新窗口打开。
    /// </summary>
    public bool OpenInNewWindow { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 路由元数据 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? MetaJson { get; set; }

    /// <summary>
    /// 角色菜单关系。
    /// </summary>
    [Navigate(nameof(AdminRoleMenu.MenuId))]
    public List<AdminRoleMenu> RoleMenus { get; set; } = [];
}

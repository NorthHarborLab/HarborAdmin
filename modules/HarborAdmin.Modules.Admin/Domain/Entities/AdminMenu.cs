using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 菜单和路由入口。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_menu_code", nameof(MenuCode), true)]
[Index("ux_admin_menu_route_path", nameof(RoutePath), true)]
public sealed class AdminMenu : EntityBase
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
    /// 绑定功能编码。
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
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 路由元数据 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? MetaJson { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

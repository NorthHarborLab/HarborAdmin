using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;

namespace HarborAdmin.Modules.Admin.Contracts.System.Request;

/// <summary>
/// 保存菜单请求。
/// </summary>
public sealed class SaveSystemMenuRequest
{
    /// <summary>
    /// 父级菜单 ID。
    /// </summary>
    [MaxLength(32)]
    public string? Pid { get; set; }

    /// <summary>
    /// 菜单名称。
    /// </summary>
    [Required(ErrorMessage = "菜单名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 路由路径。
    /// </summary>
    [MaxLength(300)]
    public string? Path { get; set; }

    /// <summary>
    /// 菜单类型。
    /// </summary>
    [Required(ErrorMessage = "菜单类型不能为空。")]
    [MaxLength(32)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 功能编码。
    /// </summary>
    [MaxLength(120)]
    public string? FeatureCode { get; set; }

    /// <summary>
    /// 组件路径。
    /// </summary>
    [MaxLength(300)]
    public string? Component { get; set; }

    /// <summary>
    /// 权限编码。
    /// </summary>
    [MaxLength(120)]
    public string? AuthCode { get; set; }

    /// <summary>
    /// 状态：1 启用，0 禁用。
    /// </summary>
    [Range(0, 1, ErrorMessage = "菜单状态不合法。")]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 菜单标题。
    /// </summary>
    [MaxLength(120)]
    public string? Title { get; set; }

    /// <summary>
    /// 菜单图标。
    /// </summary>
    [MaxLength(120)]
    public string? Icon { get; set; }

    /// <summary>
    /// 激活菜单图标。
    /// </summary>
    [MaxLength(120)]
    public string? ActiveIcon { get; set; }

    /// <summary>
    /// 激活路径。
    /// </summary>
    [MaxLength(300)]
    public string? ActivePath { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// 是否固定标签页。
    /// </summary>
    public bool AffixTab { get; set; }

    /// <summary>
    /// 固定标签页排序。
    /// </summary>
    public int? AffixTabOrder { get; set; }

    /// <summary>
    /// 是否在菜单中隐藏。
    /// </summary>
    public bool HideInMenu { get; set; }

    /// <summary>
    /// 是否在标签页中隐藏。
    /// </summary>
    public bool HideInTab { get; set; }

    /// <summary>
    /// 是否缓存页面。
    /// </summary>
    public bool KeepAlive { get; set; }

    /// <summary>
    /// 是否隐藏子菜单。
    /// </summary>
    public bool HideChildrenInMenu { get; set; }

    /// <summary>
    /// 外链地址。
    /// </summary>
    [MaxLength(500)]
    public string? Link { get; set; }

    /// <summary>
    /// iframe 地址。
    /// </summary>
    [MaxLength(500)]
    public string? IframeSrc { get; set; }

    /// <summary>
    /// 是否新窗口打开。
    /// </summary>
    public bool OpenInNewWindow { get; set; }

    /// <summary>
    /// 菜单扩展元数据 JSON。
    /// </summary>
    public string? MetaJson { get; set; }

    /// <summary>
    /// 菜单元数据（兼容旧客户端）。
    /// </summary>
    public SystemMenuMetaDto? Meta { get; set; }

    /// <summary>
    /// 重定向路径。
    /// </summary>
    [MaxLength(300)]
    public string? Redirect { get; set; }

    /// <summary>
    /// 外链地址（兼容旧客户端）。
    /// </summary>
    [MaxLength(500)]
    public string? LinkSrc { get; set; }
}

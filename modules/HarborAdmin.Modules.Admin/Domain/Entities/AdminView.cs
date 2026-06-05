using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 后台动态视图定义。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_view_code", nameof(ViewCode), true)]
public sealed class AdminView : EntityBase
{
    /// <summary>
    /// 视图编码。
    /// </summary>
    public string ViewCode { get; set; } = string.Empty;

    /// <summary>
    /// 关联资源编码。
    /// </summary>
    public string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 视图标题国际化 Key。
    /// </summary>
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// 视图标题兜底文本。
    /// </summary>
    public string? TitleFallback { get; set; }

    /// <summary>
    /// 视图类型。
    /// </summary>
    public string ViewType { get; set; } = string.Empty;

    /// <summary>
    /// 前端路由路径。
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// schema 版本号。
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间（UTC）。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

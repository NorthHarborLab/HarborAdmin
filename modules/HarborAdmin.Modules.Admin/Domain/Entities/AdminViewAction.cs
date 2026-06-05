using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 后台动态视图动作定义。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_view_action", $"{nameof(ViewCode)},{nameof(ActionCode)}", true)]
public sealed class AdminViewAction : EntityBase
{
    /// <summary>
    /// 视图编码。
    /// </summary>
    public string ViewCode { get; set; } = string.Empty;

    /// <summary>
    /// 动作编码。
    /// </summary>
    public string ActionCode { get; set; } = string.Empty;

    /// <summary>
    /// 动作标题国际化 Key。
    /// </summary>
    public string LabelKey { get; set; } = string.Empty;

    /// <summary>
    /// 动作标题兜底文本。
    /// </summary>
    public string? LabelFallback { get; set; }

    /// <summary>
    /// 权限编码。
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 展示顺序。
    /// </summary>
    public int Order { get; set; }

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

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// 功能按钮和权限点
/// </summary>
[Index("ux_admin_feature_action", $"{nameof(FeatureCode)},{nameof(ActionCode)}", true)]
[Index("ux_admin_feature_action_permission", nameof(PermissionCode), true)]
[Index("idx_admin_feature_action_feature_id", nameof(AdminFeatureId), false)]
public sealed class AdminFeatureAction : AuditableEntity
{
    /// <summary>
    /// 功能 ID。
    /// </summary>
    public long AdminFeatureId { get; set; }

    /// <summary>
    /// 所属功能。
    /// </summary>
    [Navigate(nameof(AdminFeatureId))]
    public AdminFeature AdminFeature { get; set; } = null!;

    /// <summary>
    /// 功能编码
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 操作编码
    /// </summary>
    public string ActionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限编码
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 动作标题国际化 Key
    /// </summary>
    public string LabelKey { get; set; } = string.Empty;

    /// <summary>
    /// 动作标题兜底文本
    /// </summary>
    public string? LabelFallback { get; set; }

    /// <summary>
    /// 展示顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 绑定的 API 关系。
    /// </summary>
    [Navigate(nameof(AdminFeatureActionApi.AdminFeatureActionId))]
    public List<AdminFeatureActionApi> ActionApis { get; set; } = [];
}

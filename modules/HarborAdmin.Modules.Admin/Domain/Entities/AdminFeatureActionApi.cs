using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 功能动作与 API 关系
/// </summary>
[Index("ux_admin_feature_action_api", $"{nameof(AdminFeatureActionId)},{nameof(AdminFeatureApiId)}", true)]
[Index("idx_admin_feature_action_api_feature_id", nameof(AdminFeatureId), false)]
[Index("idx_admin_feature_action_api_action_id", nameof(AdminFeatureActionId), false)]
[Index("idx_admin_feature_action_api_api_id", nameof(AdminFeatureApiId), false)]
public sealed class AdminFeatureActionApi : EntityBase
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
    /// 功能动作 ID。
    /// </summary>
    public long AdminFeatureActionId { get; set; }

    /// <summary>
    /// 功能动作。
    /// </summary>
    [Navigate(nameof(AdminFeatureActionId))]
    public AdminFeatureAction AdminFeatureAction { get; set; } = null!;

    /// <summary>
    /// 功能 API ID。
    /// </summary>
    public long AdminFeatureApiId { get; set; }

    /// <summary>
    /// 功能 API。
    /// </summary>
    [Navigate(nameof(AdminFeatureApiId))]
    public AdminFeatureApi AdminFeatureApi { get; set; } = null!;

    /// <summary>
    /// 功能编码
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 动作编码
    /// </summary>
    public string ActionCode { get; set; } = string.Empty;
}

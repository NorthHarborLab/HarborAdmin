using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 保存功能动作请求。
/// </summary>
public sealed class SaveAdminFeatureActionRequest
{
    /// <summary>
    /// 按钮/动作编码。
    /// </summary>
    [Required(ErrorMessage = "按钮编码不能为空。")]
    [MaxLength(120)]
    public string ActionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限编码。
    /// </summary>
    [Required(ErrorMessage = "权限编码不能为空。")]
    [MaxLength(120)]
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 按钮标签 I18n Key。
    /// </summary>
    [Required(ErrorMessage = "按钮标签 Key 不能为空。")]
    [MaxLength(120)]
    public string LabelKey { get; set; } = string.Empty;

    /// <summary>
    /// 按钮标签默认文案。
    /// </summary>
    public string? LabelFallback { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 关联 API Id 列表。
    /// </summary>
    public IReadOnlyList<long>? ApiIds { get; set; }
}

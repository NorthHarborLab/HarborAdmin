using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 保存 Feature 请求。
/// </summary>
public sealed class SaveAdminFeatureRequest
{
    /// <summary>
    /// 功能编码。
    /// </summary>
    [Required(ErrorMessage = "功能编码不能为空。")]
    [MaxLength(120)]
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 功能名称 I18n Key。
    /// </summary>
    [Required(ErrorMessage = "功能名称 Key 不能为空。")]
    [MaxLength(120)]
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// 功能名称默认文案。
    /// </summary>
    public string? NameFallback { get; set; }

    /// <summary>
    /// 功能类型。
    /// </summary>
    [Required(ErrorMessage = "功能类型不能为空。")]
    public string FeatureType { get; set; } = "Static";

    /// <summary>
    /// 组件标识。
    /// </summary>
    [Required(ErrorMessage = "功能组件不能为空。")]
    [MaxLength(120)]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 动态组件处理器。
    /// </summary>
    public string? HandlerKey { get; set; }

    /// <summary>
    /// 模块名称。
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 路由路径。
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

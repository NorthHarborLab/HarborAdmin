using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 保存功能 API 请求。
/// </summary>
public sealed class SaveAdminFeatureApiRequest
{
    /// <summary>
    /// API 编码。
    /// </summary>
    [Required(ErrorMessage = "API 编码不能为空。")]
    [MaxLength(120)]
    public string ApiCode { get; set; } = string.Empty;

    /// <summary>
    /// 接口名称 I18n Key。
    /// </summary>
    [Required(ErrorMessage = "接口名称 Key 不能为空。")]
    [MaxLength(120)]
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// 接口名称默认文案。
    /// </summary>
    public string? NameFallback { get; set; }

    /// <summary>
    /// 接口路径。
    /// </summary>
    [Required(ErrorMessage = "接口路径不能为空。")]
    [MaxLength(300)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 方法。
    /// </summary>
    [Required(ErrorMessage = "HTTP 方法不能为空。")]
    [RegularExpression("^(?i)(GET|POST|PUT|PATCH|DELETE)$", ErrorMessage = "HTTP 方法必须是 GET/POST/PUT/PATCH/DELETE。")]
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// 开启接口日志。
    /// </summary>
    public bool EnabledLog { get; set; }

    /// <summary>
    /// 开启参数日志。
    /// </summary>
    public bool EnabledParams { get; set; }

    /// <summary>
    /// 开启返回结果日志。
    /// </summary>
    public bool EnabledResult { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

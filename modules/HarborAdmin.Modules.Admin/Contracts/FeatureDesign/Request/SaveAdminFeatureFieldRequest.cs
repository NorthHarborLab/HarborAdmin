using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 保存功能字段请求。
/// </summary>
public sealed class SaveAdminFeatureFieldRequest
{
    /// <summary>
    /// 字段编码。
    /// </summary>
    [Required(ErrorMessage = "字段编码不能为空。")]
    [MaxLength(120)]
    public string FieldCode { get; set; } = string.Empty;

    /// <summary>
    /// 字段标签 I18n Key。
    /// </summary>
    [Required(ErrorMessage = "字段标签 Key 不能为空。")]
    [MaxLength(120)]
    public string LabelKey { get; set; } = string.Empty;

    /// <summary>
    /// 字段标签默认文案。
    /// </summary>
    public string? LabelFallback { get; set; }

    /// <summary>
    /// 占位文案 I18n Key。
    /// </summary>
    public string? PlaceholderKey { get; set; }

    /// <summary>
    /// 占位文案默认文案。
    /// </summary>
    public string? PlaceholderFallback { get; set; }

    /// <summary>
    /// 字段组件。
    /// </summary>
    [Required(ErrorMessage = "字段组件不能为空。")]
    [MaxLength(120)]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 字段数据类型。
    /// </summary>
    [Required(ErrorMessage = "字段数据类型不能为空。")]
    [MaxLength(80)]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 列表是否可见。
    /// </summary>
    public bool ListVisible { get; set; }

    /// <summary>
    /// 搜索是否可见。
    /// </summary>
    public bool SearchVisible { get; set; }

    /// <summary>
    /// 新建是否可见。
    /// </summary>
    public bool CreateVisible { get; set; }

    /// <summary>
    /// 编辑是否可见。
    /// </summary>
    public bool UpdateVisible { get; set; }

    /// <summary>
    /// 只读。
    /// </summary>
    public bool Readonly { get; set; }

    /// <summary>
    /// 是否必填。
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 字段宽度。
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// 选项 JSON。
    /// </summary>
    [JsonText]
    public string? OptionsJson { get; set; }

    /// <summary>
    /// 验证规则 JSON。
    /// </summary>
    [JsonText]
    public string? ValidationJson { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

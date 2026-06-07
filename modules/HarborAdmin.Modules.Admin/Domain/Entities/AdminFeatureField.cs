using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 功能字段与表单项。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_feature_field", $"{nameof(FeatureCode)},{nameof(FieldCode)}", true)]
[Index("idx_admin_feature_field_feature_id", nameof(AdminFeatureId), false)]
public sealed class AdminFeatureField : AuditableEntity
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
    /// 功能编码。
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 字段编码。
    /// </summary>
    public string FieldCode { get; set; } = string.Empty;

    /// <summary>
    /// 字段标题国际化 Key。
    /// </summary>
    public string LabelKey { get; set; } = string.Empty;

    /// <summary>
    /// 字段标题兜底文本。
    /// </summary>
    public string? LabelFallback { get; set; }

    /// <summary>
    /// 输入提示国际化 Key。
    /// </summary>
    public string? PlaceholderKey { get; set; }

    /// <summary>
    /// 输入提示兜底文本。
    /// </summary>
    public string? PlaceholderFallback { get; set; }

    /// <summary>
    /// 前端表单组件名称。
    /// </summary>
    public string Component { get; set; } = "Input";

    /// <summary>
    /// 字段数据类型。
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 是否在列表中展示。
    /// </summary>
    public bool ListVisible { get; set; }

    /// <summary>
    /// 是否在搜索中展示。
    /// </summary>
    public bool SearchVisible { get; set; }

    /// <summary>
    /// 是否在创建表单中展示。
    /// </summary>
    public bool CreateVisible { get; set; }

    /// <summary>
    /// 是否在更新表单中展示。
    /// </summary>
    public bool UpdateVisible { get; set; }

    /// <summary>
    /// 是否只读。
    /// </summary>
    public bool Readonly { get; set; }

    /// <summary>
    /// 是否必填。
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 展示顺序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 列表宽度。
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// 选项 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? OptionsJson { get; set; }

    /// <summary>
    /// 校验规则 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? ValidationJson { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;
}

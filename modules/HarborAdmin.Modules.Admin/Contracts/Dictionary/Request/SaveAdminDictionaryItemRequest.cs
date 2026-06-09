using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.Dictionary.Request;

/// <summary>
/// 保存字典项请求。
/// </summary>
public sealed class SaveAdminDictionaryItemRequest
{
    /// <summary>
    /// 字典项值。
    /// </summary>
    [Required(ErrorMessage = "字典项值不能为空。")]
    [MaxLength(120)]
    public string ItemValue { get; set; } = string.Empty;

    /// <summary>
    /// 字典项文本。
    /// </summary>
    [Required(ErrorMessage = "字典项文本不能为空。")]
    [MaxLength(120)]
    public string ItemLabel { get; set; } = string.Empty;

    /// <summary>
    /// 展示颜色。
    /// </summary>
    [MaxLength(80)]
    public string? Color { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;
}

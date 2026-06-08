using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.International.Contracts.Entry.Dto;

namespace HarborAdmin.Modules.International.Contracts.Entry.Request;

/// <summary>
/// 保存国际化树节点请求。
/// </summary>
public sealed class SaveInternationalEntryRequest
{
    /// <summary>
    /// 父级条目 ID（创建时使用）。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 条目键名。
    /// </summary>
    [Required(ErrorMessage = "条目键名不能为空。")]
    [MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 翻译列表。
    /// </summary>
    public IReadOnlyList<InternationalEntryTranslationDto> Translations { get; set; } = [];
}

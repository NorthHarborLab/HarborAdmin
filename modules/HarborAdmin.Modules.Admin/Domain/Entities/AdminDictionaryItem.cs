using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 字典项。
/// </summary>
[Index("ux_admin_dictionary_item", $"{nameof(DictCode)},{nameof(ItemValue)}", true)]
[Index("idx_admin_dictionary_item_dictionary_id", nameof(AdminDictionaryId), false)]
public sealed class AdminDictionaryItem : AuditableEntity
{
    /// <summary>
    /// 字典 ID。
    /// </summary>
    public long AdminDictionaryId { get; set; }

    /// <summary>
    /// 所属字典。
    /// </summary>
    [Navigate(nameof(AdminDictionaryId))]
    public AdminDictionary AdminDictionary { get; set; } = null!;

    /// <summary>
    /// 字典编码。
    /// </summary>
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典项值。
    /// </summary>
    public string ItemValue { get; set; } = string.Empty;

    /// <summary>
    /// 字典项文本。
    /// </summary>
    public string ItemLabel { get; set; } = string.Empty;

    /// <summary>
    /// 展示颜色。
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 展示顺序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;
}

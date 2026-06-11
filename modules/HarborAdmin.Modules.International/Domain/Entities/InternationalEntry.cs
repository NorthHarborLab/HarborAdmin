using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.International.Domain.Entities;

/// <summary>
/// 页面内国际化树节点。
/// </summary>
[Index("ux_intl_entry_page_parent_key", $"{nameof(PageId)},{nameof(ParentId)},{nameof(Key)}", true)]
public sealed class InternationalEntry : AuditableEntity
{
    /// <summary>
    /// 所属页面主键。
    /// </summary>
    public long PageId { get; set; }

    /// <summary>
    /// 父级节点主键。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 节点键名，例如 <c>inputModes</c>、<c>kv</c>。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 所属页面。
    /// </summary>
    [Navigate(nameof(PageId))]
    public InternationalPage? Page { get; set; }

    /// <summary>
    /// 父级节点
    /// </summary>
    [Navigate(nameof(ParentId))]
    public InternationalEntry? Parent { get; set; }

    /// <summary>
    /// 子级节点
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<InternationalEntry> Children { get; set; } = [];

    /// <summary>
    /// 多语言翻译
    /// </summary>
    [Navigate(nameof(InternationalEntryTranslation.EntryId))]
    public List<InternationalEntryTranslation> Translations { get; set; } = [];
}

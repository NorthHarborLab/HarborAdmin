using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.International.Domain.Entities;

/// <summary>
/// 国际化树节点的语言文案。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_intl_entry_locale", $"{nameof(EntryId)},{nameof(Locale)}", true)]
public sealed class InternationalEntryTranslation : EntityBase
{
    /// <summary>
    /// 所属节点主键。
    /// </summary>
    public long EntryId { get; set; }

    /// <summary>
    /// 语言标识，例如 <c>zh-CN</c>、<c>en-US</c>。
    /// </summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>
    /// 翻译文本。
    /// </summary>
    [Column(StringLength = -1)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 所属节点
    /// </summary>
    [Navigate(nameof(EntryId))]
    public InternationalEntry? Entry { get; set; }
}

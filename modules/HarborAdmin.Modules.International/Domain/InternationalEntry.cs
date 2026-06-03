using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.International.Domain;

/// <summary>
/// 页面内 国际化树节点
/// </summary>
[DbKey("AdminDb")]
public class InternationalEntry : EntityBase
{
    /// <summary>
    /// 所属页面主键
    /// </summary>
    public long PageId { get; set; }

    /// <summary>
    /// 父级节点主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 节点键名，例如 <c>inputModes</c>、<c>kv</c>
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 排序值
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 更新时间（UTC）
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// 所属页面
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

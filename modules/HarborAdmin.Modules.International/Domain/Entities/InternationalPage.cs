using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.International.Domain.Entities;

/// <summary>
/// 前端国际化页面命名空间，例如 <c>config-center</c>。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_intl_page_key", nameof(PageKey), true)]
public sealed class InternationalPage : AuditableEntity
{
    /// <summary>
    /// 前端国际化顶层命名空间。
    /// </summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>
    /// 页面国际化版本。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 页面显示名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 页面下的语言条目。
    /// </summary>
    [Navigate(nameof(InternationalEntry.PageId))]
    public List<InternationalEntry> Entries { get; set; } = [];
}

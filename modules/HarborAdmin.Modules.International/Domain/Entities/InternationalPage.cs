using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.International.Domain.Entities;

/// <summary>
/// 前端国际化页面，例如 <c>config-center/workspace/items</c>。
/// </summary>
[Index("ux_intl_page_full_path", nameof(FullPath), true)]
public sealed class InternationalPage : AuditableEntity
{
    /// <summary>
    /// 所属分组主键。
    /// </summary>
    public long? GroupId { get; set; }

    /// <summary>
    /// 页面键名，即完整路径末段。
    /// </summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>
    /// 页面完整路径，例如 <c>international/list</c>。
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

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
    /// 所属分组。
    /// </summary>
    [Navigate(nameof(GroupId))]
    public InternationalGroup? Group { get; set; }

    /// <summary>
    /// 页面下的语言条目。
    /// </summary>
    [Navigate(nameof(InternationalEntry.PageId))]
    public List<InternationalEntry> Entries { get; set; } = [];
}

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.International.Domain.Entities;

/// <summary>
/// 国际化资源分组，对齐前端 views 模块和子视图目录。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_intl_group_parent_key", $"{nameof(ParentId)},{nameof(Key)}", true)]
[Index("ux_intl_group_path", nameof(Path), true)]
public sealed class InternationalGroup : AuditableEntity
{
    /// <summary>
    /// 父级分组主键。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 分组键名，例如 <c>international</c>、<c>workspace</c>。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 从模块到当前分组的完整路径，例如 <c>config-center/workspace</c>。
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 分组显示名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序值。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 父级分组。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public InternationalGroup? Parent { get; set; }

    /// <summary>
    /// 子级分组。
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<InternationalGroup> Children { get; set; } = [];
}

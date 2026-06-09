using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 字典类型。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_dictionary_code", nameof(DictCode), true)]
public sealed class AdminDictionary : AuditableEntity
{
    /// <summary>
    /// 字典编码。
    /// </summary>
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

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

    /// <summary>
    /// 字典项。
    /// </summary>
    [Navigate(nameof(AdminDictionaryItem.AdminDictionaryId))]
    public List<AdminDictionaryItem> Items { get; set; } = [];
}

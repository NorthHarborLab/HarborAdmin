using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.Dictionary.Request;

/// <summary>
/// 保存字典类型请求。
/// </summary>
public sealed class SaveAdminDictionaryRequest
{
    /// <summary>
    /// 字典编码。
    /// </summary>
    [Required(ErrorMessage = "字典编码不能为空。")]
    [MaxLength(120)]
    public string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称。
    /// </summary>
    [Required(ErrorMessage = "字典名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

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

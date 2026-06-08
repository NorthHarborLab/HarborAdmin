using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.International.Contracts.Requests;

/// <summary>
/// 更新国际化页面请求。
/// </summary>
public sealed class UpdateInternationalPageRequest
{
    /// <summary>
    /// 页面键名。
    /// </summary>
    [Required(ErrorMessage = "页面键名不能为空。")]
    [MaxLength(120)]
    public string PageKey { get; set; } = string.Empty;

    /// <summary>
    /// 页面名称。
    /// </summary>
    [Required(ErrorMessage = "页面名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }
}

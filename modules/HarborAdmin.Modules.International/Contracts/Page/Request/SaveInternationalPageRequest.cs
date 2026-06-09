using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.International.Contracts.Page.Request;

/// <summary>
/// 保存国际化页面请求。
/// </summary>
public sealed class SaveInternationalPageRequest
{
    /// <summary>
    /// 页面完整路径。
    /// </summary>
    [Required(ErrorMessage = "页面路径不能为空。")]
    [MaxLength(120)]
    public string FullPath { get; set; } = string.Empty;

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

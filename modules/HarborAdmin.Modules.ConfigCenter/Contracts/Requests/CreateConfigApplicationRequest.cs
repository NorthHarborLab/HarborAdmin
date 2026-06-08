using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Contracts.Requests;

/// <summary>
/// 创建应用请求。
/// </summary>
public sealed class CreateConfigApplicationRequest
{
    /// <summary>
    /// 应用标识。
    /// </summary>
    [Required(ErrorMessage = "AppId 不能为空。")]
    [MaxLength(120)]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 应用名称。
    /// </summary>
    [Required(ErrorMessage = "应用名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述。
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}

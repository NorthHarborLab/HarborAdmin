using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Contracts.Application.Request;

/// <summary>
/// 保存应用请求。
/// </summary>
public sealed class SaveConfigApplicationRequest
{
    /// <summary>
    /// 应用标识（创建时必填）。
    /// </summary>
    [MaxLength(120)]
    public string? AppId { get; set; }

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

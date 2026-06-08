using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Contracts.Requests;

/// <summary>
/// 更新配置项请求。
/// </summary>
public sealed class UpdateConfigItemRequest
{
    /// <summary>
    /// 配置分组。
    /// </summary>
    [Required(ErrorMessage = "配置分组不能为空。")]
    [MaxLength(200)]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 配置键。
    /// </summary>
    [Required(ErrorMessage = "配置键不能为空。")]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值。
    /// </summary>
    [Required(ErrorMessage = "配置值不能为空。")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 值类型。
    /// </summary>
    [MaxLength(32)]
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }
}

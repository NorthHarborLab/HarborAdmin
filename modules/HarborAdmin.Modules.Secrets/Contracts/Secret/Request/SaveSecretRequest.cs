using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Secrets.Contracts.Secret.Request;

/// <summary>
/// 保存或轮换密钥请求。
/// </summary>
public sealed class SaveSecretRequest
{
    /// <summary>
    /// 密钥引用。
    /// </summary>
    [Required(ErrorMessage = "SecretRef 不能为空")]
    [MaxLength(200)]
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    [Required(ErrorMessage = "显示名称不能为空")]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 密钥明文。
    /// </summary>
    [Required(ErrorMessage = "密钥值不能为空")]
    [MaxLength(8192)]
    public string SecretValue { get; set; } = string.Empty;
}

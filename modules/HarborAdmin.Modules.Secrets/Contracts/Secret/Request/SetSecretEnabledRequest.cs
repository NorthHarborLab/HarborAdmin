using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Secrets.Contracts.Secret.Request;

/// <summary>
/// 设置密钥启停请求。
/// </summary>
public sealed class SetSecretEnabledRequest
{
    /// <summary>
    /// 密钥引用。
    /// </summary>
    [Required(ErrorMessage = "SecretRef 不能为空。")]
    [MaxLength(200)]
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

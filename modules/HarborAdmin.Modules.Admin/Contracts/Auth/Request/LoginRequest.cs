using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.Auth.Request;

/// <summary>
/// 登录请求。
/// </summary>
public sealed class LoginRequest : IValidatableObject
{
    /// <summary>
    /// 用户名。
    /// </summary>
    [Required(ErrorMessage = "请输入用户名。")]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 明文密码（仅开发环境兜底）。
    /// </summary>
    [MaxLength(128)]
    public string? Password { get; set; }

    /// <summary>
    /// RSA 加密后的密码。
    /// </summary>
    [MaxLength(4096)]
    public string? PasswordCipherText { get; set; }

    /// <summary>
    /// 密码加密挑战标识。
    /// </summary>
    [MaxLength(64)]
    public string? CryptoChallengeId { get; set; }

    /// <summary>
    /// 验证码令牌。
    /// </summary>
    [MaxLength(64)]
    public string? CaptchaToken { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(PasswordCipherText))
        {
            if (string.IsNullOrWhiteSpace(CryptoChallengeId))
            {
                yield return new ValidationResult(
                    "密码加密挑战不能为空。",
                    [nameof(CryptoChallengeId)]);
            }

            yield break;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult(
                "密码不能为空。",
                [nameof(Password), nameof(PasswordCipherText)]);
        }
    }
}

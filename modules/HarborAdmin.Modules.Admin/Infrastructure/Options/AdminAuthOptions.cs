namespace HarborAdmin.Modules.Admin.Infrastructure.Options;

/// <summary>
/// Admin 认证配置。
/// </summary>
public sealed class AdminAuthOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Harbor:AdminAuth";

    /// <summary>
    /// Access token 有效分钟数。
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>
    /// Refresh token 有效天数。
    /// </summary>
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// 是否启用验证码。
    /// </summary>
    public bool CaptchaEnabled { get; set; } = true;

    /// <summary>
    /// 验证码详细配置。
    /// </summary>
    public AdminCaptchaOptions Captcha { get; set; } = new();

    /// <summary>
    /// 开发环境是否允许禁用验证码。
    /// </summary>
    public bool AllowDisableCaptchaInDevelopment { get; set; } = true;

    /// <summary>
    /// 访问令牌签名密钥。
    /// </summary>
    public string SigningKey { get; set; } = "HarborAdmin-Development-Signing-Key-Change-Me";
}

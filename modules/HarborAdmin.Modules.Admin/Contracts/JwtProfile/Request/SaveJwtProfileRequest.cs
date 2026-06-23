using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.JwtProfile.Request;

/// <summary>
/// 保存 JWT Profile 请求。
/// </summary>
public sealed class SaveJwtProfileRequest
{
    /// <summary>
    /// Profile Key。
    /// </summary>
    [Required(ErrorMessage = "Profile Key 不能为空。")]
    [MaxLength(120)]
    public string ProfileKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    [Required(ErrorMessage = "显示名称不能为空。")]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 用途。
    /// </summary>
    [Required(ErrorMessage = "用途不能为空。")]
    [MaxLength(64)]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// 签发方。
    /// </summary>
    [Required(ErrorMessage = "Issuer 不能为空。")]
    [MaxLength(200)]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 接收方。
    /// </summary>
    [Required(ErrorMessage = "Audience 不能为空。")]
    [MaxLength(200)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 签名密钥引用。
    /// </summary>
    [Required(ErrorMessage = "Secret Ref 不能为空。")]
    [MaxLength(200)]
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 签名密钥版本。
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Secret 版本必须大于 0。")]
    public int SecretVersion { get; set; } = 1;

    /// <summary>
    /// Access token 有效分钟数。
    /// </summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>
    /// Refresh token 有效天数。
    /// </summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// 时钟偏移秒数。
    /// </summary>
    [Range(0, 600)]
    public int ClockSkewSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 说明。
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
}

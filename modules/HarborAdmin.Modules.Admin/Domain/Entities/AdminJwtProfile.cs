using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// JWT 凭证配置。
/// </summary>
[Index("ux_admin_jwt_profile_key", nameof(ProfileKey), true)]
public sealed class AdminJwtProfile : AuditableEntity
{
    /// <summary>
    /// Profile Key。
    /// </summary>
    public string ProfileKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 用途。
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// 签发方。
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 接收方。
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法。
    /// </summary>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// 签名密钥引用。
    /// </summary>
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 签名密钥版本。
    /// </summary>
    public int SecretVersion { get; set; }

    /// <summary>
    /// Access token 有效分钟数。
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>
    /// Refresh token 有效天数。
    /// </summary>
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// 时钟偏移秒数。
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 说明。
    /// </summary>
    [Column(StringLength = -1)]
    public string? Description { get; set; }
}

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// JWT Profile 刷新令牌。
/// </summary>
[Index("ux_admin_jwt_refresh_token_hash", nameof(TokenHash), true)]
[Index("idx_admin_jwt_refresh_token_subject", $"{nameof(ProfileKey)},{nameof(Subject)}", false)]
public sealed class AdminJwtRefreshToken : EntityBase
{
    /// <summary>
    /// JWT Profile Key。
    /// </summary>
    public string ProfileKey { get; set; } = string.Empty;

    /// <summary>
    /// 主体标识。
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 主体类型。
    /// </summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌哈希。
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 吊销时间。
    /// </summary>
    [Column(IsNullable = true)]
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 替换后的刷新令牌哈希。
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 创建来源 IP。
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// User-Agent。
    /// </summary>
    [Column(StringLength = -1)]
    public string? UserAgent { get; set; }
}

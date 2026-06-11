using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 刷新令牌。
/// </summary>
[Index("ux_admin_refresh_token_hash", nameof(TokenHash), true)]
public sealed class AdminRefreshToken : EntityBase
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户。
    /// </summary>
    [Navigate(nameof(UserId))]
    public AdminUser User { get; set; } = null!;

    /// <summary>
    /// 令牌哈希。
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
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

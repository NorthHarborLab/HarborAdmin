using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// RSA 加密挑战缓存模型。
/// </summary>
[CacheCatalog("RSA 加密挑战", GroupPrefix = "harbor:admin:auth", GroupName = "Admin 认证", Module = "Admin", Order = 100, SupportsBulkClear = false, SensitiveFields = ["PrivateKeyBase64"])]
[CacheKey("harbor:admin:crypto-challenge", Key = "{ChallengeId}", ExpirationSeconds = 120)]
public sealed class CryptoChallengeCacheModel
{
    /// <summary>
    /// 挑战 ID。
    /// </summary>
    [CacheKeyPart]
    public required string ChallengeId { get; init; }

    /// <summary>
    /// RSA 私钥（Base64）。
    /// </summary>
    public required string PrivateKeyBase64 { get; init; }

    /// <summary>
    /// 过期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}

using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// 验证码令牌缓存模型。
/// </summary>
[CacheCatalog("验证码令牌", GroupPrefix = "harbor:admin:auth", GroupName = "Admin 认证", Module = "Admin", Order = 102, SupportsBulkClear = false, SensitiveFields = ["Token"])]
[CacheKey("harbor:admin:captcha-token", Key = "{Token}", ExpirationSeconds = 120)]
public sealed class CaptchaTokenCacheModel
{
    /// <summary>
    /// 验证码令牌。
    /// </summary>
    [CacheKeyPart]
    public required string Token { get; init; }

    /// <summary>
    /// 过期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}

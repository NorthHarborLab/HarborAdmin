using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// 验证码挑战缓存模型。
/// </summary>
[CacheCatalog("验证码挑战", GroupPrefix = "harbor:admin:auth", GroupName = "Admin 认证", Module = "Admin", Order = 101, SupportsBulkClear = false, SensitiveFields = ["Regions", "PieceX", "PieceY", "InitialDegree", "HintText"])]
[CacheKey("harbor:admin:captcha-challenge", Key = "{CaptchaId}", ExpirationSeconds = 120)]
public sealed class CaptchaChallengeCacheModel
{
    /// <summary>
    /// 挑战 ID。
    /// </summary>
    [CacheKeyPart]
    public required string CaptchaId { get; init; }

    /// <summary>
    /// 挑战类型。
    /// </summary>
    public CaptchaChallengeKind Kind { get; init; }

    /// <summary>
    /// 过期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// 点选提示文字。
    /// </summary>
    public string? HintText { get; init; }

    /// <summary>
    /// 点选字符区域。
    /// </summary>
    public CaptchaCharRegionCacheModel[]? Regions { get; init; }

    /// <summary>
    /// 旋转初始角度。
    /// </summary>
    public int? InitialDegree { get; init; }

    /// <summary>
    /// 拼图 X 坐标。
    /// </summary>
    public int? PieceX { get; init; }

    /// <summary>
    /// 拼图 Y 坐标。
    /// </summary>
    public int? PieceY { get; init; }
}

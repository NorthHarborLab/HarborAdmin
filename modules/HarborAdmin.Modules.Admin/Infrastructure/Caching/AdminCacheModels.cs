using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// RSA 加密挑战缓存模型。
/// </summary>
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

/// <summary>
/// 验证码挑战类型。
/// </summary>
public enum CaptchaChallengeKind
{
    /// <summary>
    /// 点选文字。
    /// </summary>
    Point,

    /// <summary>
    /// 滑块拖动。
    /// </summary>
    Slider,

    /// <summary>
    /// 旋转图片。
    /// </summary>
    Rotate,

    /// <summary>
    /// 拼图滑块。
    /// </summary>
    Translate,
}

/// <summary>
/// 点选验证码字符区域缓存模型。
/// </summary>
public sealed class CaptchaCharRegionCacheModel
{
    /// <summary>
    /// 区域左上角 X 坐标。
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// 区域左上角 Y 坐标。
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// 区域宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// 区域高度。
    /// </summary>
    public int Height { get; init; }
}

/// <summary>
/// 验证码挑战缓存模型。
/// </summary>
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

/// <summary>
/// 验证码令牌缓存模型。
/// </summary>
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

using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.Modules.Admin.Application.Captcha;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.Auth.Request;
using HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Modules.Admin.Application.Services.Captcha;

/// <summary>
/// 统一验证码挑战与校验服务。
/// </summary>
/// <remarks>
/// 按配置生成点选、滑块、旋转、拼图四类验证码，校验通过后颁发一次性登录令牌。
/// </remarks>
public sealed class CaptchaChallengeService(IOptions<AdminAuthOptions> authOptions, IWebHostEnvironment environment, CaptchaImagePool imagePool, IHarborCache cache)
{
    /// <summary>
    /// 按当前配置创建验证码挑战。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>验证码挑战数据；未启用时返回 <c>Enabled = false</c> 的占位挑战。</returns>
    public async Task<CaptchaChallengeDto> CreateChallengeAsync(CancellationToken cancellationToken)
    {
        var enabled = IsCaptchaEnabled();
        var captchaOptions = authOptions.Value.Captcha;
        var id = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(captchaOptions.ChallengeMinutes);
        var type = captchaOptions.Type;

        if (!enabled)
        {
            return BuildDisabledChallenge(id, type, expiresAt);
        }

        return type switch
        {
            CaptchaType.Point => await CreatePointChallengeAsync(id, expiresAt, captchaOptions, cancellationToken),
            CaptchaType.Slider => await CreateSliderChallengeAsync(id, expiresAt, cancellationToken),
            CaptchaType.SliderRotate => await CreateRotateChallengeAsync(id, expiresAt, captchaOptions, cancellationToken),
            CaptchaType.SliderTranslate => await CreateTranslateChallengeAsync(id, expiresAt, captchaOptions, cancellationToken),
            _ => await CreatePointChallengeAsync(id, expiresAt, captchaOptions, cancellationToken),
        };
    }

    /// <summary>
    /// 校验验证码并颁发 token。
    /// </summary>
    /// <param name="request">验证码校验请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>验证码校验结果。</returns>
    public async Task<VerifyCaptchaResult> VerifyChallengeAsync(
        VerifyCaptchaRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsCaptchaEnabled())
        {
            return new VerifyCaptchaResult("captcha-disabled");
        }

        var state = await cache.TryConsumeAsync<CaptchaChallengeCacheModel>(
            model => model.CaptchaId == request.CaptchaId,
            cancellationToken);
        if (state is null || state.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationDomainException("验证码已过期，请刷新后重试。");
        }

        if (!ValidateChallenge(state, request))
        {
            throw new ValidationDomainException(GetVerifyFailureMessage(state.Kind));
        }

        var token = Guid.NewGuid().ToString("N");
        var tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(authOptions.Value.Captcha.ChallengeMinutes);
        var expiration = TimeSpan.FromMinutes(authOptions.Value.Captcha.ChallengeMinutes);
        await cache.SetAsync(
            model => model.Token == token,
            new CaptchaTokenCacheModel { Token = token, ExpiresAt = tokenExpiresAt },
            expiration,
            cancellationToken);
        return new VerifyCaptchaResult(token);
    }

    /// <summary>
    /// 消费验证码 token。
    /// </summary>
    /// <param name="token">验证码令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ConsumeCaptchaTokenAsync(string? token, CancellationToken cancellationToken)
    {
        if (!IsCaptchaEnabled())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationDomainException("验证码已过期或无效。");
        }

        var state = await cache.TryConsumeAsync<CaptchaTokenCacheModel>(
            model => model.Token == token,
            cancellationToken);
        if (state is null || state.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationDomainException("验证码已过期或无效。");
        }
    }

    /// <summary>
    /// 创建点选验证码挑战。
    /// </summary>
    private async Task<CaptchaChallengeDto> CreatePointChallengeAsync(
        string id,
        DateTimeOffset expiresAt,
        AdminCaptchaOptions captchaOptions,
        CancellationToken cancellationToken)
    {
        var imageBytes = imagePool.PickRandom(Random.Shared);
        var layout = PointCaptchaGenerator.Create(true, captchaOptions, imageBytes);
        await StoreChallengeAsync(
            id,
            new CaptchaChallengeCacheModel
            {
                CaptchaId = id,
                Kind = CaptchaChallengeKind.Point,
                ExpiresAt = expiresAt,
                HintText = layout.HintText,
                Regions = ToCacheRegions(layout.Regions),
            },
            captchaOptions.ChallengeMinutes,
            cancellationToken);
        return new CaptchaChallengeDto(
            id,
            ToApiType(CaptchaType.Point),
            true,
            expiresAt,
            layout.ImageDataUri,
            layout.HintText);
    }

    /// <summary>
    /// 创建滑块验证码挑战。
    /// </summary>
    private async Task<CaptchaChallengeDto> CreateSliderChallengeAsync(
        string id,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await StoreChallengeAsync(
            id,
            new CaptchaChallengeCacheModel
            {
                CaptchaId = id,
                Kind = CaptchaChallengeKind.Slider,
                ExpiresAt = expiresAt,
            },
            authOptions.Value.Captcha.ChallengeMinutes,
            cancellationToken);
        return new CaptchaChallengeDto(id, ToApiType(CaptchaType.Slider), true, expiresAt);
    }

    /// <summary>
    /// 创建旋转验证码挑战。
    /// </summary>
    private async Task<CaptchaChallengeDto> CreateRotateChallengeAsync(
        string id,
        DateTimeOffset expiresAt,
        AdminCaptchaOptions captchaOptions,
        CancellationToken cancellationToken)
    {
        var imageBytes = imagePool.PickRandomForRotate(Random.Shared);
        var layout = RotateCaptchaGenerator.Create(imageBytes, captchaOptions, Random.Shared);
        await StoreChallengeAsync(
            id,
            new CaptchaChallengeCacheModel
            {
                CaptchaId = id,
                Kind = CaptchaChallengeKind.Rotate,
                ExpiresAt = expiresAt,
                InitialDegree = layout.InitialDegree,
            },
            captchaOptions.ChallengeMinutes,
            cancellationToken);
        return new CaptchaChallengeDto(id, ToApiType(CaptchaType.SliderRotate), true, expiresAt, null, null,
            layout.ImageDataUri, captchaOptions.RotateImageSize, layout.InitialDegree, captchaOptions.RotateDiffDegree);
    }

    /// <summary>
    /// 创建拼图验证码挑战。
    /// </summary>
    private async Task<CaptchaChallengeDto> CreateTranslateChallengeAsync(
        string id,
        DateTimeOffset expiresAt,
        AdminCaptchaOptions captchaOptions,
        CancellationToken cancellationToken)
    {
        var imageBytes = imagePool.PickRandom(Random.Shared);
        var layout = TranslateCaptchaGenerator.Create(imageBytes, captchaOptions, Random.Shared);
        await StoreChallengeAsync(
            id,
            new CaptchaChallengeCacheModel
            {
                CaptchaId = id,
                Kind = CaptchaChallengeKind.Translate,
                ExpiresAt = expiresAt,
                PieceX = layout.PieceX,
                PieceY = layout.PieceY,
            },
            captchaOptions.ChallengeMinutes,
            cancellationToken);
        return new CaptchaChallengeDto(id, ToApiType(CaptchaType.SliderTranslate), true, expiresAt, null, null, null,
            null, null, null, layout.BackgroundImageDataUri, layout.PieceImageDataUri,
            captchaOptions.TranslateCanvasWidth, captchaOptions.TranslateCanvasHeight,
            captchaOptions.TranslateDiffDistance);
    }

    /// <summary>
    /// 写入验证码挑战缓存。
    /// </summary>
    private ValueTask StoreChallengeAsync(
        string id,
        CaptchaChallengeCacheModel model,
        int challengeMinutes,
        CancellationToken cancellationToken) =>
        cache.SetAsync(
            item => item.CaptchaId == id,
            model,
            TimeSpan.FromMinutes(challengeMinutes),
            cancellationToken);

    /// <summary>
    /// 创建禁用验证码挑战。
    /// </summary>
    private CaptchaChallengeDto BuildDisabledChallenge(string id, CaptchaType type, DateTimeOffset expiresAt)
    {
        if (type == CaptchaType.Point)
        {
            var layout = PointCaptchaGenerator.Create(false, authOptions.Value.Captcha);
            return new CaptchaChallengeDto(id, ToApiType(type), false, expiresAt, layout.ImageDataUri, layout.HintText);
        }

        return new CaptchaChallengeDto(id, ToApiType(type), false, expiresAt);
    }

    /// <summary>
    /// 校验验证码挑战。
    /// </summary>
    private bool ValidateChallenge(CaptchaChallengeCacheModel state, VerifyCaptchaRequest request) =>
        state.Kind switch
        {
            CaptchaChallengeKind.Point => PointCaptchaGenerator.ValidatePoints(
                request.Points ?? [],
                ToGeneratorRegions(state.Regions),
                authOptions.Value.Captcha.PointTolerance),
            CaptchaChallengeKind.Slider => ValidateSlider(request.DurationSeconds),
            CaptchaChallengeKind.Rotate => request.CurrentRotate is { } currentRotate
                                           && state.InitialDegree is { } initialDegree
                                           && RotateCaptchaGenerator.Validate(
                                               initialDegree,
                                               currentRotate,
                                               authOptions.Value.Captcha.RotateDiffDegree),
            CaptchaChallengeKind.Translate => request.MoveDistance is { } moveDistance
                                              && state.PieceX is { } pieceX
                                              && TranslateCaptchaGenerator.Validate(
                                                  pieceX,
                                                  moveDistance,
                                                  authOptions.Value.Captcha.TranslateDiffDistance),
            _ => false,
        };

    /// <summary>
    /// 验证滑块。
    /// </summary>
    private bool ValidateSlider(double? durationSeconds)
    {
        if (durationSeconds is not { } duration)
        {
            return false;
        }

        var options = authOptions.Value.Captcha;
        return duration >= options.SliderMinSeconds && duration <= options.SliderMaxSeconds;
    }

    /// <summary>
    /// 是否启用验证码。
    /// </summary>
    private bool IsCaptchaEnabled() =>
        authOptions.Value.CaptchaEnabled
        && (!environment.IsDevelopment() || !authOptions.Value.AllowDisableCaptchaInDevelopment);

    /// <summary>
    /// 获取验证失败消息。
    /// </summary>
    private static string GetVerifyFailureMessage(CaptchaChallengeKind kind) =>
        kind switch
        {
            CaptchaChallengeKind.Point => "验证码校验失败，请按提示依次点击文字。",
            CaptchaChallengeKind.Slider => "滑块验证失败，请匀速拖动滑块至末端。",
            CaptchaChallengeKind.Rotate => "旋转验证失败，请调整图片至正确角度。",
            CaptchaChallengeKind.Translate => "拼图验证失败，请拖动滑块对齐缺口。",
            _ => "验证码校验失败。",
        };

    /// <summary>
    /// 转换为 API 类型。
    /// </summary>
    private static string ToApiType(CaptchaType type) =>
        type switch
        {
            CaptchaType.Point => "point",
            CaptchaType.Slider => "slider",
            CaptchaType.SliderRotate => "sliderRotate",
            CaptchaType.SliderTranslate => "sliderTranslate",
            _ => "point",
        };

    /// <summary>
    /// 将生成器区域映射为缓存模型。
    /// </summary>
    private static CaptchaCharRegionCacheModel[] ToCacheRegions(PointCaptchaGenerator.CaptchaCharRegion[] regions) =>
        regions.Select(region => new CaptchaCharRegionCacheModel
        {
            X = region.X,
            Y = region.Y,
            Width = region.Width,
            Height = region.Height,
        }).ToArray();

    /// <summary>
    /// 将缓存区域映射为生成器区域。
    /// </summary>
    private static PointCaptchaGenerator.CaptchaCharRegion[] ToGeneratorRegions(CaptchaCharRegionCacheModel[]? regions) =>
        regions?.Select(region => new PointCaptchaGenerator.CaptchaCharRegion(
            region.X,
            region.Y,
            region.Width,
            region.Height)).ToArray() ?? [];
}
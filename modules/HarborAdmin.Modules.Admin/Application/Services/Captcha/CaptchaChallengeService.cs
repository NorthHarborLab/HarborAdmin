using System.Collections.Concurrent;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.Auth.Request;
using HarborAdmin.Modules.Admin.Application.Captcha;
using HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;
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
public sealed class CaptchaChallengeService(IOptions<AdminAuthOptions> authOptions, IWebHostEnvironment environment, CaptchaImagePool imagePool)
{
    /// <summary>
    /// 内存中的验证码挑战缓存，键为挑战 ID。
    /// </summary>
    private static readonly ConcurrentDictionary<string, CaptchaChallengeState> Challenges = new();

    /// <summary>
    /// 内存中的验证码令牌缓存，校验通过后供登录消费。
    /// </summary>
    private static readonly ConcurrentDictionary<string, CaptchaTokenState> Tokens = new();

    /// <summary>
    /// 按当前配置创建验证码挑战。
    /// </summary>
    /// <returns>验证码挑战数据；未启用时返回 <c>Enabled = false</c> 的占位挑战。</returns>
    public CaptchaChallengeDto CreateChallenge()
    {
        CleanupCaches();
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
            CaptchaType.Point => CreatePointChallenge(id, expiresAt, captchaOptions),
            CaptchaType.Slider => CreateSliderChallenge(id, expiresAt),
            CaptchaType.SliderRotate => CreateRotateChallenge(id, expiresAt, captchaOptions),
            CaptchaType.SliderTranslate => CreateTranslateChallenge(id, expiresAt, captchaOptions),
            _ => CreatePointChallenge(id, expiresAt, captchaOptions),
        };
    }

    /// <summary>
    /// 校验验证码并颁发 token。
    /// </summary>
    public VerifyCaptchaResult VerifyChallenge(VerifyCaptchaRequest request)
    {
        if (!IsCaptchaEnabled())
        {
            return new VerifyCaptchaResult("captcha-disabled");
        }

        if (!Challenges.TryRemove(request.CaptchaId, out var state) || state.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationDomainException("验证码已过期，请刷新后重试。");
        }

        var valid = state switch
        {
            PointCaptchaChallengeState point => PointCaptchaGenerator.ValidatePoints(
                request.Points ?? [],
                point.Regions,
                authOptions.Value.Captcha.PointTolerance),
            SliderCaptchaChallengeState => ValidateSlider(request.DurationSeconds),
            RotateCaptchaChallengeState rotate => request.CurrentRotate is { } currentRotate
                                                  && RotateCaptchaGenerator.Validate(
                                                      rotate.InitialDegree,
                                                      currentRotate,
                                                      authOptions.Value.Captcha.RotateDiffDegree),
            TranslateCaptchaChallengeState translate => request.MoveDistance is { } moveDistance
                                                        && TranslateCaptchaGenerator.Validate(
                                                            translate.PieceX,
                                                            moveDistance,
                                                            authOptions.Value.Captcha.TranslateDiffDistance),
            _ => false,
        };

        if (!valid)
        {
            throw new ValidationDomainException(GetVerifyFailureMessage(state));
        }

        var token = Guid.NewGuid().ToString("N");
        Tokens[token] =
            new CaptchaTokenState(DateTimeOffset.UtcNow.AddMinutes(authOptions.Value.Captcha.ChallengeMinutes));
        return new VerifyCaptchaResult(token);
    }

    /// <summary>
    /// 消费验证码 token。
    /// </summary>
    public void ConsumeCaptchaToken(string? token)
    {
        if (!IsCaptchaEnabled())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(token)
            || !Tokens.TryRemove(token, out var state)
            || state.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationDomainException("验证码已过期或无效。");
        }
    }

    /// <summary>
    /// 创建点选验证码挑战
    /// </summary>
    /// <param name="id">挑战 ID</param>
    /// <param name="expiresAt">挑战过期时间</param>
    /// <param name="captchaOptions">验证码选项</param>
    /// <returns>验证码挑战数据</returns>
    private CaptchaChallengeDto CreatePointChallenge(string id, DateTimeOffset expiresAt, AdminCaptchaOptions captchaOptions)
    {
        var imageBytes = imagePool.PickRandom(Random.Shared);
        var layout = PointCaptchaGenerator.Create(true, captchaOptions, imageBytes);
        Challenges[id] = new PointCaptchaChallengeState(layout.HintText, layout.Regions, expiresAt);
        return new CaptchaChallengeDto(
            id,
            ToApiType(CaptchaType.Point),
            true,
            expiresAt,
            layout.ImageDataUri,
            layout.HintText);
    }

    /// <summary>
    /// 创建滑块验证码挑战
    /// </summary>
    /// <param name="id">挑战 ID</param>
    /// <param name="expiresAt">挑战过期时间</param>
    /// <returns>验证码挑战数据</returns>
    private CaptchaChallengeDto CreateSliderChallenge(string id, DateTimeOffset expiresAt)
    {
        Challenges[id] = new SliderCaptchaChallengeState(expiresAt);
        return new CaptchaChallengeDto(id, ToApiType(CaptchaType.Slider), true, expiresAt);
    }

    /// <summary>
    /// 创建旋转验证码挑战
    /// </summary>
    /// <param name="id">挑战 ID</param>
    /// <param name="expiresAt">挑战过期时间</param>
    /// <param name="captchaOptions">验证码选项</param>
    /// <returns>验证码挑战数据</returns>
    private CaptchaChallengeDto CreateRotateChallenge(string id, DateTimeOffset expiresAt, AdminCaptchaOptions captchaOptions)
    {
        var imageBytes = imagePool.PickRandomForRotate(Random.Shared);
        var layout = RotateCaptchaGenerator.Create(imageBytes, captchaOptions, Random.Shared);
        Challenges[id] = new RotateCaptchaChallengeState(layout.InitialDegree, expiresAt);
        return new CaptchaChallengeDto(id, ToApiType(CaptchaType.SliderRotate), true, expiresAt, null, null,
            layout.ImageDataUri, captchaOptions.RotateImageSize, layout.InitialDegree, captchaOptions.RotateDiffDegree);
    }

    /// <summary>
    /// 创建拼图验证码挑战
    /// </summary>
    /// <param name="id">挑战 ID</param>
    /// <param name="expiresAt">挑战过期时间</param>
    /// <param name="captchaOptions">验证码选项</param>
    /// <returns>验证码挑战数据</returns>
    private CaptchaChallengeDto CreateTranslateChallenge(string id, DateTimeOffset expiresAt, AdminCaptchaOptions captchaOptions)
    {
        var imageBytes = imagePool.PickRandom(Random.Shared);
        var layout = TranslateCaptchaGenerator.Create(imageBytes, captchaOptions, Random.Shared);
        Challenges[id] = new TranslateCaptchaChallengeState(layout.PieceX, layout.PieceY, expiresAt);
        return new CaptchaChallengeDto(id, ToApiType(CaptchaType.SliderTranslate), true, expiresAt, null, null, null,
            null, null, null, layout.BackgroundImageDataUri, layout.PieceImageDataUri,
            captchaOptions.TranslateCanvasWidth, captchaOptions.TranslateCanvasHeight,
            captchaOptions.TranslateDiffDistance);
    }

    /// <summary>
    /// 创建禁用验证码挑战
    /// </summary>
    /// <param name="id">挑战 ID</param>
    /// <param name="type">验证码类型</param>
    /// <param name="expiresAt">挑战过期时间</param>
    /// <returns>验证码挑战数据</returns>
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
    /// 验证滑块
    /// </summary>
    /// <param name="durationSeconds">滑动时间</param>
    /// <returns>是否验证成功</returns>
    /// <returns></returns>
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
    /// 是否启用验证码
    /// </summary>
    /// <returns>是否启用验证码</returns>
    private bool IsCaptchaEnabled() =>
        authOptions.Value.CaptchaEnabled
        && (!environment.IsDevelopment() || !authOptions.Value.AllowDisableCaptchaInDevelopment);

    /// <summary>
    /// 获取验证失败消息
    /// </summary>
    /// <param name="state">验证码挑战状态</param>
    /// <returns>验证失败消息</returns>
    /// <returns></returns>
    private static string GetVerifyFailureMessage(CaptchaChallengeState state) =>
        state switch
        {
            PointCaptchaChallengeState => "验证码校验失败，请按提示依次点击文字。",
            SliderCaptchaChallengeState => "滑块验证失败，请匀速拖动滑块至末端。",
            RotateCaptchaChallengeState => "旋转验证失败，请调整图片至正确角度。",
            TranslateCaptchaChallengeState => "拼图验证失败，请拖动滑块对齐缺口。",
            _ => "验证码校验失败。",
        };

    /// <summary>
    /// 转换为 API 类型
    /// </summary>
    /// <param name="type">验证码类型</param>
    /// <returns>API 类型</returns>
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
    /// 清理缓存
    /// </summary>
    private static void CleanupCaches()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in Challenges.Where(pair => pair.Value.ExpiresAt < now).ToArray())
        {
            Challenges.TryRemove(pair.Key, out _);
        }

        foreach (var pair in Tokens.Where(pair => pair.Value.ExpiresAt < now).ToArray())
        {
            Tokens.TryRemove(pair.Key, out _);
        }
    }

    /// <summary>
    /// 验证码挑战状态
    /// </summary>
    private abstract record CaptchaChallengeState(DateTimeOffset ExpiresAt);

    /// <summary>
    /// 点选验证码挑战结构
    /// </summary>
    private sealed record PointCaptchaChallengeState(string HintText, PointCaptchaGenerator.CaptchaCharRegion[] Regions, DateTimeOffset ExpiresAt)
        : CaptchaChallengeState(ExpiresAt);

    /// <summary>
    /// 滑块验证码挑战结构
    /// </summary>
    private sealed record SliderCaptchaChallengeState(DateTimeOffset ExpiresAt) : CaptchaChallengeState(ExpiresAt);

    /// <summary>
    /// 旋转验证码挑战结构
    /// </summary>
    private sealed record RotateCaptchaChallengeState(int InitialDegree, DateTimeOffset ExpiresAt) : CaptchaChallengeState(ExpiresAt);

    /// <summary>
    /// 拼图验证码挑战结构
    /// </summary>
    private sealed record TranslateCaptchaChallengeState(int PieceX, int PieceY, DateTimeOffset ExpiresAt) : CaptchaChallengeState(ExpiresAt);

    /// <summary>
    /// 验证码令牌结构
    /// </summary>
    private sealed record CaptchaTokenState(DateTimeOffset ExpiresAt);
}
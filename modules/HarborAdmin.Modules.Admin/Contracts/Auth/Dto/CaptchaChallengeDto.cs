namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// 统一验证码挑战
/// </summary>
public sealed record CaptchaChallengeDto(
    string CaptchaId,
    string Type,
    bool Enabled,
    DateTimeOffset ExpiresAt,
    string? CaptchaImage,
    string? HintText,
    string? RotateImage,
    int? RotateImageSize,
    int? RotateInitialDegree,
    int? RotateDiffDegree,
    string? TranslateBackgroundImage,
    string? TranslatePieceImage,
    int? TranslateCanvasWidth,
    int? TranslateCanvasHeight,
    int? TranslateDiffDistance);

namespace HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;

/// <summary>
/// 统一验证码挑战
/// </summary>
public sealed class CaptchaChallengeDto
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="captchaId">挑战 ID</param>
    /// <param name="type">挑战类型</param>
    /// <param name="enabled">是否启用</param>
    /// <param name="expiresAt">挑战过期时间</param>
    /// <param name="captchaImage">验证码图片</param>
    /// <param name="hintText">提示文本</param>
    /// <param name="rotateImage">旋转图片</param>
    /// <param name="rotateImageSize">旋转图片大小</param>
    /// <param name="rotateInitialDegree">旋转初始角度</param>
    /// <param name="rotateDiffDegree">旋转差异角度</param>
    /// <param name="translateBackgroundImage">拼图背景图片</param>
    /// <param name="translatePieceImage">拼图碎片图片</param>
    /// <param name="translateCanvasWidth">拼图画布宽度</param>
    /// <param name="translateCanvasHeight">拼图画布高度</param>
    /// <param name="translateDiffDistance">拼图差异距离</param>
    public CaptchaChallengeDto(string captchaId, string type, bool enabled, DateTimeOffset expiresAt,
        string? captchaImage = null, string? hintText = null, string? rotateImage = null, int? rotateImageSize = null,
        int? rotateInitialDegree = null,
        int? rotateDiffDegree = null, string? translateBackgroundImage = null, string? translatePieceImage = null,
        int? translateCanvasWidth = null,
        int? translateCanvasHeight = null, int? translateDiffDistance = null)
    {
        CaptchaId = captchaId;
        Type = type;
        Enabled = enabled;
        ExpiresAt = expiresAt;
        CaptchaImage = captchaImage;
        HintText = hintText;
        RotateImage = rotateImage;
        RotateImageSize = rotateImageSize;
        RotateInitialDegree = rotateInitialDegree;
        RotateDiffDegree = rotateDiffDegree;
        TranslateBackgroundImage = translateBackgroundImage;
        TranslatePieceImage = translatePieceImage;
        TranslateCanvasWidth = translateCanvasWidth;
        TranslateCanvasHeight = translateCanvasHeight;
        TranslateDiffDistance = translateDiffDistance;
    }

    /// <summary>
    /// 挑战 ID
    /// </summary>
    public string CaptchaId { get; set; }

    /// <summary>
    /// 挑战类型
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 挑战过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 验证码图片
    /// </summary>
    public string? CaptchaImage { get; set; }

    /// <summary>
    /// 提示文本
    /// </summary>
    public string? HintText { get; set; }

    /// <summary>
    /// 旋转图片
    /// </summary>
    public string? RotateImage { get; set; }

    /// <summary>
    /// 旋转图片大小
    /// </summary>
    public int? RotateImageSize { get; set; }

    /// <summary>
    /// 旋转初始角度
    /// </summary>
    public int? RotateInitialDegree { get; set; }

    /// <summary>
    /// 旋转差异角度
    /// </summary>
    public int? RotateDiffDegree { get; set; }

    /// <summary>
    /// 拼图背景图片
    /// </summary>
    public string? TranslateBackgroundImage { get; set; }

    /// <summary>
    /// 拼图碎片图片
    /// </summary>
    public string? TranslatePieceImage { get; set; }

    /// <summary>
    /// 拼图画布宽度
    /// </summary>
    public int? TranslateCanvasWidth { get; set; }

    /// <summary>
    /// 拼图画布高度
    /// </summary>
    public int? TranslateCanvasHeight { get; set; }

    /// <summary>
    /// 拼图差异距离
    /// </summary>
    public int? TranslateDiffDistance { get; set; }
}
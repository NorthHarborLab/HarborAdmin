namespace HarborAdmin.Modules.Admin.Infrastructure.Options;

/// <summary>
/// 验证码类型。
/// </summary>
public enum CaptchaType
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
    SliderRotate,

    /// <summary>
    /// 拼图滑块。
    /// </summary>
    SliderTranslate,
}

/// <summary>
/// Admin 验证码配置。
/// </summary>
public sealed class AdminCaptchaOptions
{
    /// <summary>
    /// 验证码类型。
    /// </summary>
    public CaptchaType Type { get; set; } = CaptchaType.Point;

    /// <summary>
    /// 挑战有效分钟数。
    /// </summary>
    public int ChallengeMinutes { get; set; } = 2;

    /// <summary>
    /// 图片资源池相对目录（基于应用程序根目录）。
    /// </summary>
    public string ImagePoolPath { get; set; } = "Infrastructure/Assets/Captcha";

    /// <summary>
    /// 点选最少字符数。
    /// </summary>
    public int PointMinChars { get; set; } = 2;

    /// <summary>
    /// 点选最多字符数。
    /// </summary>
    public int PointMaxChars { get; set; } = 3;

    /// <summary>
    /// 点选点击容差（像素）。
    /// </summary>
    public int PointTolerance { get; set; } = 35;

    /// <summary>
    /// 点选文字池（每项为一个汉字，未配置时使用内置默认字库）。
    /// </summary>
    public string[] PointCharPool { get; set; } = [];

    /// <summary>
    /// 点选文字池（连续汉字字符串，<see cref="PointCharPool"/> 为空时生效）。
    /// </summary>
    public string? PointCharPoolText { get; set; }

    /// <summary>
    /// 滑块最短耗时（秒）。
    /// </summary>
    public double SliderMinSeconds { get; set; } = 0.3;

    /// <summary>
    /// 滑块最长耗时（秒）。
    /// </summary>
    public double SliderMaxSeconds { get; set; } = 30;

    /// <summary>
    /// 旋转图片边长（像素）。
    /// </summary>
    public int RotateImageSize { get; set; } = 260;

    /// <summary>
    /// 旋转初始角度下限。
    /// </summary>
    public int RotateMinDegree { get; set; } = 120;

    /// <summary>
    /// 旋转初始角度上限。
    /// </summary>
    public int RotateMaxDegree { get; set; } = 300;

    /// <summary>
    /// 旋转校验容差（度）。
    /// </summary>
    public int RotateDiffDegree { get; set; } = 20;

    /// <summary>
    /// 拼图画布宽度。
    /// </summary>
    public int TranslateCanvasWidth { get; set; } = 420;

    /// <summary>
    /// 拼图画布高度。
    /// </summary>
    public int TranslateCanvasHeight { get; set; } = 280;

    /// <summary>
    /// 拼图方块边长。
    /// </summary>
    public int TranslateSquareLength { get; set; } = 42;

    /// <summary>
    /// 拼图凸起圆半径。
    /// </summary>
    public int TranslateCircleRadius { get; set; } = 10;

    /// <summary>
    /// 拼图水平容差（像素）。
    /// </summary>
    public int TranslateDiffDistance { get; set; } = 3;
}

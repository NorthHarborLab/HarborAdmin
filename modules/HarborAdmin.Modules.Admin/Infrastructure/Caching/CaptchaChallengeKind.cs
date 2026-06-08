namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

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

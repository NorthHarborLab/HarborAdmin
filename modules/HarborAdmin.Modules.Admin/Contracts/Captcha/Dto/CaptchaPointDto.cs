using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;

/// <summary>
/// 点选坐标。
/// </summary>
public sealed class CaptchaPointDto
{
    /// <summary>
    /// 横坐标。
    /// </summary>
    [Range(0, 10_000, ErrorMessage = "点击横坐标不合法。")]
    public int X { get; set; }

    /// <summary>
    /// 纵坐标。
    /// </summary>
    [Range(0, 10_000, ErrorMessage = "点击纵坐标不合法。")]
    public int Y { get; set; }

    /// <summary>
    /// 时间戳。
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "点击时间戳不合法。")]
    public long T { get; set; }

    /// <summary>
    /// 点击序号。
    /// </summary>
    [Range(0, 100, ErrorMessage = "点击序号不合法。")]
    public int I { get; set; }
}

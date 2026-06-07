using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.Auth.Request;

/// <summary>
/// 统一验证码校验请求。
/// </summary>
public sealed class VerifyCaptchaRequest
{
    /// <summary>
    /// 验证码标识。
    /// </summary>
    [Required(ErrorMessage = "验证码标识不能为空。")]
    [MaxLength(64)]
    public string CaptchaId { get; set; } = string.Empty;

    /// <summary>
    /// 点选坐标集合。
    /// </summary>
    public List<CaptchaPointDto>? Points { get; set; }

    /// <summary>
    /// 滑块耗时（秒）。
    /// </summary>
    [Range(0, 300, ErrorMessage = "滑块耗时不在允许范围内。")]
    public double? DurationSeconds { get; set; }

    /// <summary>
    /// 当前旋转角度。
    /// </summary>
    [Range(-360, 720, ErrorMessage = "旋转角度不合法。")]
    public int? CurrentRotate { get; set; }

    /// <summary>
    /// 拼图水平位移。
    /// </summary>
    [Range(0, 10_000, ErrorMessage = "拼图位移不合法。")]
    public int? MoveDistance { get; set; }
}

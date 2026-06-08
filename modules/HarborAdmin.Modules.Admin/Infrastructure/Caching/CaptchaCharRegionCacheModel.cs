namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

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

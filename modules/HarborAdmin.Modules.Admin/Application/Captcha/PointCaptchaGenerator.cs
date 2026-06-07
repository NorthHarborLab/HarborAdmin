using System.Text;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.Auth.Request;
using HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using SkiaSharp;

namespace HarborAdmin.Modules.Admin.Application.Captcha;

/// <summary>
/// 点选验证码图片生成与坐标校验。
/// </summary>
/// <remarks>
/// 在背景图上随机摆放提示汉字，前端按顺序点击后由 <see cref="ValidatePoints"/> 校验坐标。
/// </remarks>
internal static class PointCaptchaGenerator
{
    /// <summary>
    /// 验证码画布宽度（像素）。
    /// </summary>
    private const int CanvasWidth = 300;

    /// <summary>
    /// 验证码画布高度（像素）。
    /// </summary>
    private const int CanvasHeight = 180;

    /// <summary>
    /// 提示文字字号。
    /// </summary>
    private const int FontSize = 38;

    /// <summary>
    /// 单字点击区域宽度。
    /// </summary>
    private const int CharWidth = 38;

    /// <summary>
    /// 单字点击区域高度。
    /// </summary>
    private const int CharHeight = 38;

    /// <summary>
    /// 默认点击容差（像素）。
    /// </summary>
    private const int DefaultClickTolerance = 35;

    /// <summary>
    /// 内置默认汉字字库。
    /// </summary>
    private static readonly string[] DefaultCharPool =
    [
        "港", "湾", "海", "洋", "帆", "船", "灯", "塔", "桥", "城",
        "山", "水", "云", "风", "雨", "星", "月", "日", "树", "花",
        "鱼", "鸟", "石", "沙", "波", "潮", "岸", "岛", "旗", "路",
    ];

    /// <summary>
    /// 提示文字填充色候选列表。
    /// </summary>
    private static readonly SKColor[] TextFillColors =
    [
        new(255, 251, 235, 255),
        new(254, 243, 199, 255),
        new(254, 226, 226, 255),
        new(220, 252, 231, 255),
        new(224, 231, 255, 255),
        new(254, 249, 195, 255),
        new(255, 228, 230, 255),
    ];

    /// <summary>
    /// 点选验证码布局结果。
    /// </summary>
    /// <param name="HintText">需按顺序点击的提示文字。</param>
    /// <param name="Regions">各字符在画布上的点击区域。</param>
    /// <param name="ImageDataUri">合成后的验证码图片 Data URI。</param>
    internal sealed record CaptchaLayout(string HintText, CaptchaCharRegion[] Regions, string ImageDataUri);

    /// <summary>
    /// 单个汉字的矩形点击区域。
    /// </summary>
    /// <param name="X">左上角 X 坐标。</param>
    /// <param name="Y">左上角 Y 坐标。</param>
    /// <param name="Width">区域宽度。</param>
    /// <param name="Height">区域高度。</param>
    internal sealed record CaptchaCharRegion(int X, int Y, int Width, int Height);

    /// <summary>
    /// 生成随机点选验证码布局与图片。
    /// </summary>
    /// <param name="enabled">是否启用验证码；<see langword="false"/> 时返回开发占位图。</param>
    /// <param name="options">点选验证码配置。</param>
    /// <param name="backgroundImageBytes">背景图片字节；启用时必须提供。</param>
    /// <param name="random">随机数生成器。</param>
    /// <returns>验证码布局。</returns>
    internal static CaptchaLayout Create(bool enabled, AdminCaptchaOptions? options = null, byte[]? backgroundImageBytes = null, Random? random = null)
    {
        random ??= Random.Shared;
        options ??= new AdminCaptchaOptions();
        if (!enabled)
        {
            return new CaptchaLayout("DEV", [], BuildDisabledImage());
        }

        if (backgroundImageBytes is null || backgroundImageBytes.Length == 0)
        {
            throw new InvalidOperationException("点选验证码需要背景图片。");
        }

        var minChars = Math.Max(1, Math.Min(options.PointMinChars, options.PointMaxChars));
        var maxChars = Math.Max(minChars, Math.Max(options.PointMinChars, options.PointMaxChars));
        var charPool = ResolveCharPool(options);
        if (charPool.Length < maxChars)
        {
            throw new InvalidOperationException(
                $"点选文字池至少需要 {maxChars} 个不重复汉字，当前为 {charPool.Length} 个。");
        }

        var charCount = random.Next(minChars, maxChars + 1);
        var hintText = PickRandomHintText(random, charCount, charPool);
        var regions = PlaceCharacters(hintText, random);
        var image = BuildCaptchaImage(hintText, regions, backgroundImageBytes, random);
        return new CaptchaLayout(hintText, regions, image);
    }

    /// <summary>
    /// 按顺序校验用户点击坐标是否落在目标字符区域内。
    /// </summary>
    /// <param name="points">用户点击坐标列表。</param>
    /// <param name="regions">服务端生成的字符区域列表。</param>
    /// <param name="tolerance">点击容差（像素）；为空时使用默认值。</param>
    /// <returns>全部命中时返回 <see langword="true"/>。</returns>
    internal static bool ValidatePoints(IReadOnlyList<CaptchaPointDto> points, IReadOnlyList<CaptchaCharRegion> regions, int? tolerance = null)
    {
        if (points.Count < regions.Count || regions.Count == 0)
        {
            return false;
        }

        var clickTolerance = tolerance ?? DefaultClickTolerance;
        var orderedPoints = points.OrderBy(point => point.I).ToArray();
        for (var index = 0; index < regions.Count; index++)
        {
            var region = regions[index];
            var point = orderedPoints[index];
            if (point.X < region.X - clickTolerance
                || point.X > region.X + region.Width + clickTolerance
                || point.Y < region.Y - clickTolerance
                || point.Y > region.Y + region.Height + clickTolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 解析点选文字字库：优先配置数组，其次配置文本，最后回退内置字库。
    /// </summary>
    /// <param name="options">验证码配置。</param>
    /// <returns>去重后的单字数组。</returns>
    private static string[] ResolveCharPool(AdminCaptchaOptions options)
    {
        if (options.PointCharPool is { Length: > 0 })
        {
            var configured = options.PointCharPool
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .SelectMany(SplitToSingleChars)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (configured.Length > 0)
            {
                return configured;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.PointCharPoolText))
        {
            var configured = SplitToSingleChars(options.PointCharPoolText)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (configured.Length > 0)
            {
                return configured;
            }
        }

        return DefaultCharPool;
    }

    /// <summary>
    /// 将字符串拆分为单个非空白字符。
    /// </summary>
    /// <param name="value">原始字符串。</param>
    /// <returns>单字序列。</returns>
    private static IEnumerable<string> SplitToSingleChars(string value)
    {
        foreach (var character in value)
        {
            if (!char.IsWhiteSpace(character))
            {
                yield return character.ToString();
            }
        }
    }

    /// <summary>
    /// 从字库中随机抽取不重复汉字组成提示文字。
    /// </summary>
    /// <param name="random">随机数生成器。</param>
    /// <param name="count">提示字数。</param>
    /// <param name="charPool">字库。</param>
    /// <returns>提示文字。</returns>
    private static string PickRandomHintText(Random random, int count, string[] charPool)
    {
        var pool = charPool.ToArray();
        Shuffle(pool, random);
        return string.Concat(pool.Take(count));
    }

    /// <summary>
    /// 为提示文字中的每个字符分配不重叠的点击区域。
    /// </summary>
    /// <param name="hintText">提示文字。</param>
    /// <param name="random">随机数生成器。</param>
    /// <returns>字符区域数组。</returns>
    private static CaptchaCharRegion[] PlaceCharacters(string hintText, Random random)
    {
        var placed = new List<CaptchaCharRegion>();
        foreach (var _ in hintText)
        {
            var region = TryPlaceCharacter(placed, random) ?? FallbackRegion(placed.Count);
            placed.Add(region);
        }

        return placed.ToArray();
    }

    /// <summary>
    /// 尝试在画布上随机放置一个不与已有区域重叠的字符区域。
    /// </summary>
    /// <param name="placed">已放置区域列表。</param>
    /// <param name="random">随机数生成器。</param>
    /// <returns>放置成功时返回区域；失败时返回 <see langword="null"/>。</returns>
    private static CaptchaCharRegion? TryPlaceCharacter(List<CaptchaCharRegion> placed, Random random)
    {
        const int margin = 10;
        const int bottomReserved = 16;
        var maxX = CanvasWidth - CharWidth - margin;
        var minBaselineY = margin + CharHeight;
        var maxBaselineY = CanvasHeight - margin - bottomReserved;

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var x = random.Next(margin, maxX + 1);
            var baselineY = random.Next(minBaselineY, maxBaselineY + 1);
            var candidate = new CaptchaCharRegion(x, baselineY - CharHeight, CharWidth, CharHeight);
            if (placed.All(item => !Overlaps(candidate, item)))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// 按网格顺序生成兜底字符区域，避免随机放置失败时无法生成验证码。
    /// </summary>
    /// <param name="index">字符序号。</param>
    /// <returns>网格区域。</returns>
    private static CaptchaCharRegion FallbackRegion(int index)
    {
        const int margin = 16;
        var columns = Math.Max(1, (CanvasWidth - margin * 2) / (CharWidth + 20));
        var row = index / columns;
        var column = index % columns;
        var x = margin + column * (CharWidth + 20);
        var y = margin + row * (CharHeight + 18);
        return new CaptchaCharRegion(x, y, CharWidth, CharHeight);
    }

    /// <summary>
    /// 判断两个字符区域是否重叠（含间距缓冲）。
    /// </summary>
    /// <param name="left">左侧区域。</param>
    /// <param name="right">右侧区域。</param>
    /// <returns>重叠时返回 <see langword="true"/>。</returns>
    private static bool Overlaps(CaptchaCharRegion left, CaptchaCharRegion right)
    {
        const int gap = 8;
        return left.X - gap < right.X + right.Width + gap
               && left.X + left.Width + gap > right.X - gap
               && left.Y - gap < right.Y + right.Height + gap
               && left.Y + left.Height + gap > right.Y - gap;
    }

    /// <summary>
    /// 将背景图缩放并叠加提示汉字，输出 JPEG Data URI。
    /// </summary>
    /// <param name="hintText">提示文字。</param>
    /// <param name="regions">字符区域列表。</param>
    /// <param name="backgroundImageBytes">背景图片字节。</param>
    /// <param name="random">随机数生成器，用于旋转角度与文字颜色。</param>
    /// <returns>验证码图片 Data URI。</returns>
    private static string BuildCaptchaImage(string hintText, IReadOnlyList<CaptchaCharRegion> regions, byte[] backgroundImageBytes, Random random)
    {
        using var source = SKBitmap.Decode(backgroundImageBytes)
                           ?? throw new InvalidOperationException("无法解码点选验证码背景图。");
        using var resized = source.Resize(
                                new SKImageInfo(CanvasWidth, CanvasHeight),
                                SKSamplingOptions.Default)
                            ?? throw new InvalidOperationException("无法缩放点选验证码背景图。");

        using var canvasBitmap = new SKBitmap(CanvasWidth, CanvasHeight);
        using (var canvas = new SKCanvas(canvasBitmap))
        {
            canvas.DrawBitmap(resized, 0, 0);
            using (var overlayPaint = new SKPaint())
            {
                overlayPaint.Color = new SKColor(0, 0, 0, 48);
                canvas.DrawRect(0, 0, CanvasWidth, CanvasHeight, overlayPaint);
            }

            var typeface = SKFontManager.Default.MatchCharacter('港')
                           ?? SKTypeface.FromFamilyName("Microsoft YaHei")
                           ?? SKTypeface.FromFamilyName("SimHei")
                           ?? SKTypeface.Default;

            using var font = new SKFont(typeface, FontSize);
            font.Edging = SKFontEdging.Antialias;

            for (var index = 0; index < hintText.Length && index < regions.Count; index++)
            {
                var region = regions[index];
                var baselineY = region.Y + CharHeight - 4;
                var rotation = random.Next(-18, 19);
                var centerX = region.X + CharWidth / 2f;
                var centerY = region.Y + CharHeight / 2f;
                var character = hintText[index].ToString();
                var fillColor = TextFillColors[random.Next(TextFillColors.Length)];

                using var strokePaint = new SKPaint();
                strokePaint.Color = new SKColor(15, 23, 42, 220);
                strokePaint.IsAntialias = true;
                strokePaint.Style = SKPaintStyle.Stroke;
                strokePaint.StrokeWidth = 3;
                using var fillPaint = new SKPaint();
                fillPaint.Color = fillColor;
                fillPaint.IsAntialias = true;
                fillPaint.Style = SKPaintStyle.Fill;

                canvas.Save();
                canvas.RotateDegrees(rotation, centerX, centerY);
                canvas.DrawText(character, region.X, baselineY, font, strokePaint);
                canvas.DrawText(character, region.X, baselineY, font, fillPaint);
                canvas.Restore();
            }
        }

        using var image = SKImage.FromBitmap(canvasBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 88)
                         ?? throw new InvalidOperationException("无法编码点选验证码图片。");
        return $"data:image/jpeg;base64,{Convert.ToBase64String(data.ToArray())}";
    }

    /// <summary>
    /// 生成验证码未启用时的开发占位 SVG 图片。
    /// </summary>
    /// <returns>占位图 Data URI。</returns>
    private static string BuildDisabledImage()
    {
        const string svg = """
                           <svg xmlns="http://www.w3.org/2000/svg" width="300" height="180">
                             <rect width="300" height="180" fill="#eef2ff"/>
                             <text x="118" y="96" font-size="28" font-family="Arial, sans-serif" fill="#64748b">DEV</text>
                           </svg>
                           """;
        return ToDataUri(svg);
    }

    /// <summary>
    /// 将 SVG 字符串编码为 Data URI。
    /// </summary>
    /// <param name="svg">SVG 内容。</param>
    /// <returns>SVG Data URI。</returns>
    private static string ToDataUri(string svg) =>
        $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";

    /// <summary>
    /// 使用 Fisher-Yates 算法原地打乱数组。
    /// </summary>
    /// <param name="items">待打乱数组。</param>
    /// <param name="random">随机数生成器。</param>
    private static void Shuffle(string[] items, Random random)
    {
        for (var index = items.Length - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }
}
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using SkiaSharp;

namespace HarborAdmin.Modules.Admin.Application.Captcha;

/// <summary>
/// 拼图滑块验证码图片生成与位移校验。
/// </summary>
/// <remarks>
/// 在背景图上裁切带凹凸边缘的拼图块，前端拖动滑块对齐缺口后由 <see cref="Validate"/> 校验水平位移。
/// </remarks>
internal static class TranslateCaptchaGenerator
{
    /// <summary>
    /// 拼图验证码布局结果。
    /// </summary>
    /// <param name="BackgroundImageDataUri">带缺口遮罩的背景图 Data URI。</param>
    /// <param name="PieceImageDataUri">可拖动的拼图块 Data URI。</param>
    /// <param name="PieceX">拼图块目标水平位置（像素）。</param>
    /// <param name="PieceY">拼图块垂直位置（像素）。</param>
    internal sealed record TranslateLayout(string BackgroundImageDataUri, string PieceImageDataUri, int PieceX, int PieceY);

    /// <summary>
    /// 根据背景图生成拼图验证码布局与图片。
    /// </summary>
    /// <param name="imageBytes">背景图片字节。</param>
    /// <param name="options">拼图验证码配置（画布尺寸、方块边长、圆角半径等）。</param>
    /// <param name="random">随机数生成器，用于确定拼图块位置。</param>
    /// <returns>拼图布局。</returns>
    internal static TranslateLayout Create(byte[] imageBytes, AdminCaptchaOptions options, Random random)
    {
        var canvasWidth = options.TranslateCanvasWidth;
        var canvasHeight = options.TranslateCanvasHeight;
        var squareLength = options.TranslateSquareLength;
        var circleRadius = options.TranslateCircleRadius;

        using var source = SKBitmap.Decode(imageBytes)
            ?? throw new InvalidOperationException("无法解码验证码图片。");
        using var resized = source.Resize(
            new SKImageInfo(canvasWidth, canvasHeight),
            SKSamplingOptions.Default)
            ?? throw new InvalidOperationException("无法缩放验证码图片。");

        var pieceX = random.Next(
            squareLength + 2 * circleRadius,
            canvasWidth - (squareLength + 2 * circleRadius));
        var pieceY = random.Next(
            3 * circleRadius,
            canvasHeight - (squareLength + 2 * circleRadius));
        var pieceLength = squareLength + 2 * circleRadius + 3;

        using var background = resized.Copy();
        using (var canvas = new SKCanvas(background))
        using (var fillPaint = new SKPaint())
        {
            fillPaint.Color = new SKColor(255, 255, 255, 178);
            fillPaint.IsAntialias = true;
            fillPaint.Style = SKPaintStyle.Fill;
            using (var strokePaint = new SKPaint())
            {
                strokePaint.Color = new SKColor(255, 255, 255, 178);
                strokePaint.IsAntialias = true;
                strokePaint.Style = SKPaintStyle.Stroke;
                strokePaint.StrokeWidth = 2;
                using var path = BuildPuzzlePath(pieceX, pieceY, squareLength, circleRadius);
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, strokePaint);
            }
        }

        using var piece = new SKBitmap(pieceLength, canvasHeight);
        using (var canvas = new SKCanvas(piece))
        {
            canvas.Clear(SKColors.Transparent);
            using var path = BuildPuzzlePath(0, pieceY, squareLength, circleRadius);
            canvas.Save();
            canvas.ClipPath(path);
            canvas.DrawBitmap(resized, -pieceX, 0);
            canvas.Restore();
        }

        return new TranslateLayout(
            ToDataUri(EncodePng(background)),
            ToDataUri(EncodePng(piece)),
            pieceX,
            pieceY);
    }

    /// <summary>
    /// 校验用户拖动距离是否与拼图块目标水平位置匹配。
    /// </summary>
    /// <param name="pieceX">服务端记录的目标 X 坐标。</param>
    /// <param name="moveDistance">用户拖动的水平位移。</param>
    /// <param name="diffDistance">允许误差（像素）。</param>
    /// <returns>在容差范围内时返回 <see langword="true"/>。</returns>
    internal static bool Validate(int pieceX, int moveDistance, int diffDistance) =>
        Math.Abs(pieceX - moveDistance) < diffDistance;

    /// <summary>
    /// 构建带上下凹凸边缘的拼图块路径，与前端 Canvas 算法保持一致。
    /// </summary>
    /// <param name="x">路径左上角 X 坐标。</param>
    /// <param name="y">路径左上角 Y 坐标。</param>
    /// <param name="squareLength">方块边长。</param>
    /// <param name="circleRadius">凹凸圆弧半径。</param>
    /// <returns>拼图块 Skia 路径。</returns>
    private static SKPath BuildPuzzlePath(int x, int y, int squareLength, int circleRadius)
    {
        var path = new SKPath();
        path.MoveTo(x, y);
        path.ArcTo(
            new SKRect(x + squareLength / 2f - circleRadius, y - circleRadius * 2 + 2, x + squareLength / 2f + circleRadius, y + 2),
            180,
            120,
            false);
        path.LineTo(x + squareLength, y);
        path.ArcTo(
            new SKRect(x + squareLength - 2, y + squareLength / 2f - circleRadius, x + squareLength + circleRadius * 2 - 2, y + squareLength / 2f + circleRadius),
            270,
            120,
            false);
        path.LineTo(x + squareLength, y + squareLength);
        path.LineTo(x, y + squareLength);
        path.ArcTo(
            new SKRect(x - circleRadius, y + squareLength / 2f - circleRadius - 0.4f, x + circleRadius - 2, y + squareLength / 2f + circleRadius + 0.4f),
            90,
            120,
            false);
        path.Close();
        return path;
    }

    /// <summary>
    /// 将位图编码为 PNG 字节数组。
    /// </summary>
    /// <param name="bitmap">待编码位图。</param>
    /// <returns>PNG 字节。</returns>
    private static byte[] EncodePng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("无法编码拼图验证码图片。");
        return data.ToArray();
    }

    /// <summary>
    /// 将 PNG 字节编码为 Data URI。
    /// </summary>
    /// <param name="bytes">PNG 字节。</param>
    /// <returns>PNG Data URI。</returns>
    private static string ToDataUri(byte[] bytes) =>
        $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
}

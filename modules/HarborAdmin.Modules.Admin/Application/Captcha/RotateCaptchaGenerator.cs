using HarborAdmin.Modules.Admin.Infrastructure.Options;
using SkiaSharp;

namespace HarborAdmin.Modules.Admin.Application.Captcha;

/// <summary>
/// 旋转验证码图片生成。
/// </summary>
internal static class RotateCaptchaGenerator
{
    /// <summary>
    /// 旋转验证码布局。
    /// </summary>
    internal sealed record RotateLayout(string ImageDataUri, int InitialDegree);

    /// <summary>
    /// 生成旋转验证码图片与初始角度。
    /// </summary>
    internal static RotateLayout Create(byte[] imageBytes, AdminCaptchaOptions options, Random random)
    {
        using var source = SKBitmap.Decode(imageBytes)
            ?? throw new InvalidOperationException("无法解码验证码图片。");
        var cropSize = Math.Min(source.Width, source.Height);
        var cropX = (source.Width - cropSize) / 2;
        var cropY = (source.Height - cropSize) / 2;

        using var cropped = new SKBitmap(cropSize, cropSize);
        using (var canvas = new SKCanvas(cropped))
        {
            canvas.DrawBitmap(
                source,
                new SKRect(cropX, cropY, cropX + cropSize, cropY + cropSize),
                new SKRect(0, 0, cropSize, cropSize));
        }

        using var resized = cropped.Resize(
            new SKImageInfo(options.RotateImageSize, options.RotateImageSize),
            SKSamplingOptions.Default)
            ?? throw new InvalidOperationException("无法缩放验证码图片。");

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85)
            ?? throw new InvalidOperationException("无法编码验证码图片。");

        var minDegree = Math.Min(options.RotateMinDegree, options.RotateMaxDegree);
        var maxDegree = Math.Max(options.RotateMinDegree, options.RotateMaxDegree);
        var initialDegree = random.Next(minDegree, maxDegree + 1);
        return new RotateLayout(ToDataUri(data.ToArray()), initialDegree);
    }

    /// <summary>
    /// 校验旋转角度。
    /// </summary>
    internal static bool Validate(int initialDegree, int currentRotate, int diffDegree) =>
        Math.Abs(initialDegree - currentRotate) < diffDegree;

    /// <summary>
    /// 将 JPEG 字节编码为 Data URI。
    /// </summary>
    private static string ToDataUri(byte[] bytes) =>
        $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
}

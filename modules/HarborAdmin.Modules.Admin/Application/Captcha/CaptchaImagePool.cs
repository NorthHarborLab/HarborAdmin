using System.Reflection;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Modules.Admin.Application.Captcha;

/// <summary>
/// 验证码图片资源池。
/// </summary>
public sealed class CaptchaImagePool(IOptions<AdminAuthOptions> authOptions)
{
    /// <summary>
    /// 旋转子文件夹
    /// </summary>
    private const string RotateSubfolder = "rotate";

    /// <summary>
    /// 程序集
    /// </summary>
    private static readonly Assembly Assembly = typeof(CaptchaImagePool).Assembly;

    /// <summary>
    /// 通用图片名称
    /// </summary>
    private static readonly Lazy<string[]> GeneralImageNames = new(() => LoadEmbeddedImageNames(null));

    /// <summary>
    /// 旋转图片名称
    /// </summary>
    private static readonly Lazy<string[]> RotateImageNames = new(() => LoadEmbeddedImageNames(RotateSubfolder));

    /// <summary>
    /// 随机选取一张图片字节（拼图/通用）。
    /// </summary>
    internal byte[] PickRandom(Random random) => PickRandomInternal(random, GeneralImageNames.Value, null);

    /// <summary>
    /// 随机选取一张适合旋转验证码的方形图片。
    /// </summary>
    internal byte[] PickRandomForRotate(Random random) =>
        PickRandomInternal(random, RotateImageNames.Value, RotateSubfolder);

    /// <summary>
    /// 优先从外部文件池选图，缺省时回退嵌入资源。
    /// </summary>
    /// <param name="random">随机数生成器。</param>
    /// <param name="embeddedNames">当前场景可用的嵌入资源名称。</param>
    /// <param name="subfolder">可选资源子目录。</param>
    /// <returns>图片字节。</returns>
    /// <exception cref="InvalidOperationException">验证码图片池为空或嵌入资源无法读取。</exception>
    private byte[] PickRandomInternal(Random random, string[] embeddedNames, string? subfolder)
    {
        var fileCandidate = LoadFileCandidate(random, subfolder);
        if (fileCandidate is not null)
        {
            return fileCandidate;
        }

        var names = embeddedNames.Length > 0 ? embeddedNames : GeneralImageNames.Value;
        if (names.Length == 0)
        {
            throw new InvalidOperationException("验证码图片池为空，请检查 EmbeddedResource 或 ImagePoolPath 配置。");
        }

        var resourceName = names[random.Next(names.Length)];
        using var stream = Assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"无法读取验证码图片资源：{resourceName}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>
    /// 从配置的图片池目录中随机读取一张候选图片。
    /// </summary>
    /// <param name="random">随机数生成器。</param>
    /// <param name="subfolder">可选资源子目录。</param>
    /// <returns>找到候选图时返回图片字节，否则返回 <see langword="null"/>。</returns>
    private byte[]? LoadFileCandidate(Random random, string? subfolder)
    {
        var relativePath = authOptions.Value.Captcha.ImagePoolPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var directories = new[]
        {
            string.IsNullOrWhiteSpace(subfolder)
                ? Path.Combine(AppContext.BaseDirectory, relativePath)
                : Path.Combine(AppContext.BaseDirectory, relativePath, subfolder),
            string.IsNullOrWhiteSpace(subfolder)
                ? Path.Combine(Directory.GetCurrentDirectory(), relativePath)
                : Path.Combine(Directory.GetCurrentDirectory(), relativePath, subfolder),
        };

        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            // 同时查 AppContext.BaseDirectory 与当前工作目录，兼容 dotnet run、发布目录和测试执行目录。
            var files = Directory.GetFiles(directory)
                .Where(IsSupportedImage)
                .ToArray();
            if (files.Length > 0)
            {
                return File.ReadAllBytes(files[random.Next(files.Length)]);
            }
        }

        return null;
    }

    /// <summary>
    /// 按场景加载嵌入的验证码图片资源名称。
    /// </summary>
    /// <param name="subfolder">可选资源子目录。</param>
    /// <returns>嵌入资源名称集合。</returns>
    private static string[] LoadEmbeddedImageNames(string? subfolder)
    {
        return Assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Infrastructure.Assets.Captcha.", StringComparison.Ordinal))
            .Where(name => string.IsNullOrWhiteSpace(subfolder)
                ? !name.Contains(".Infrastructure.Assets.Captcha.rotate.", StringComparison.Ordinal)
                : name.Contains($".Infrastructure.Assets.Captcha.{subfolder}.", StringComparison.Ordinal))
            .Where(IsSupportedResourceName)
            .ToArray();
    }

    /// <summary>
    /// 判断文件路径是否为支持的验证码图片格式。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>支持时返回 <see langword="true"/>。</returns>
    private static bool IsSupportedImage(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断嵌入资源名称是否为支持的验证码图片格式。
    /// </summary>
    /// <param name="resourceName">嵌入资源名称。</param>
    /// <returns>支持时返回 <see langword="true"/>。</returns>
    private static bool IsSupportedResourceName(string resourceName)
    {
        return resourceName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || resourceName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
               || resourceName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || resourceName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }
}

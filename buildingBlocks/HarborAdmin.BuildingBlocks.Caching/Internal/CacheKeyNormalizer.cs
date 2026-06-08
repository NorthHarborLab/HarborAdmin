using HarborAdmin.BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.BuildingBlocks.Caching.Internal;

/// <summary>
/// 将 Harbor:Cache:KeyPrefix 应用到缓存 key 与 tag。
/// </summary>
internal sealed class CacheKeyNormalizer(IOptions<HarborCacheOptions> options)
{
    /// <summary>
    /// 为缓存 key 或 tag 应用全局前缀。
    /// </summary>
    public string ApplyPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cache key or tag cannot be empty.", nameof(value));
        }

        var prefix = NormalizePrefix(options.Value.KeyPrefix);
        var trimmed = value.Trim();
        if (prefix.Length == 0)
        {
            return trimmed;
        }

        if (trimmed.StartsWith(prefix + ":", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return prefix + ":" + trimmed.TrimStart(':');
    }

    /// <summary>
    /// 为多个 tag 应用全局前缀。
    /// </summary>
    public IReadOnlyList<string> ApplyPrefix(IReadOnlyCollection<string> tags) =>
        tags.Select(ApplyPrefix).ToArray();

    private static string NormalizePrefix(string? prefix) =>
        (prefix ?? string.Empty).Trim().TrimEnd(':');
}

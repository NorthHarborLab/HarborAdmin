using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Internal;
using HarborAdmin.BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// Harbor 缓存运维管理实现。
/// </summary>
internal sealed class HarborCacheManager(
    ICacheCatalogProvider catalogProvider,
    ITagIndexStore tagIndexStore,
    IHarborCache cache,
    IHarborCacheInvalidator invalidator,
    CacheKeyNormalizer keyNormalizer,
    IOptions<HarborCacheOptions> options) : IHarborCacheManager
{
    private const int MaxContentBytes = 256 * 1024;

    /// <inheritdoc />
    public CacheProviderInfo GetProviderInfo() =>
        new(options.Value.Provider.ToString(), options.Value.KeyPrefix);

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CacheGroupDescriptor>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var activeTagCounts = await BuildActiveTagCountsAsync(cancellationToken);
        return catalogProvider.BuildGroups(activeTagCounts);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CacheTagRuntimeInfo>> GetActiveTagsAsync(string? groupPrefix, CancellationToken cancellationToken = default)
    {
        var normalizedGroupPrefix = string.IsNullOrWhiteSpace(groupPrefix) ? groupPrefix : keyNormalizer.ApplyPrefix(groupPrefix);
        var tags = await tagIndexStore.ListTagsAsync(normalizedGroupPrefix, cancellationToken);
        var result = new List<CacheTagRuntimeInfo>(tags.Count);
        foreach (var tag in tags)
        {
            var keys = await tagIndexStore.GetKeysAsync(tag, cancellationToken);
            result.Add(new CacheTagRuntimeInfo(tag, keys.Count));
        }

        return result;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        tagIndexStore.GetKeysAsync(keyNormalizer.ApplyPrefix(tag), cancellationToken);

    /// <inheritdoc />
    public async ValueTask<CacheEntryContent> GetEntryContentAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = keyNormalizer.ApplyPrefix(key);
        var raw = await cache.TryGetRawEntryAsync(normalizedKey, cancellationToken);
        if (raw is null || !raw.Found || string.IsNullOrEmpty(raw.Json))
        {
            return new CacheEntryContent(normalizedKey, false, null, null, 0, false);
        }

        var model = catalogProvider.MatchModelByKey(normalizedKey);
        var masked = CacheEntryMasker.MaskJson(raw.Json, model?.SensitiveFields ?? []);
        var (json, truncated) = CacheEntryMasker.TruncateIfNeeded(masked, MaxContentBytes);
        return new CacheEntryContent(normalizedKey, true, model?.ModelTypeName, json, raw.SizeBytes, truncated);
    }

    /// <inheritdoc />
    public ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default) =>
        invalidator.InvalidateTagAsync(tag, cancellationToken);

    /// <inheritdoc />
    public ValueTask InvalidateKeyAsync(string key, CancellationToken cancellationToken = default) =>
        invalidator.InvalidateKeyAsync(key, cancellationToken);

    /// <inheritdoc />
    public async ValueTask InvalidateGroupAsync(string groupPrefix, CancellationToken cancellationToken = default)
    {
        var normalizedGroupPrefix = keyNormalizer.ApplyPrefix(groupPrefix);
        var groups = catalogProvider.BuildGroups(new Dictionary<string, int>());
        var group = groups.FirstOrDefault(item => string.Equals(item.GroupPrefix, normalizedGroupPrefix, StringComparison.Ordinal));
        if (group is null)
        {
            return;
        }

        var tagsToInvalidate = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in group.Models)
        {
            foreach (var template in model.TagTemplates.Where(template => !template.Contains('{', StringComparison.Ordinal)))
            {
                tagsToInvalidate.Add(template);
            }
        }

        var runtimeTags = await tagIndexStore.ListTagsAsync(normalizedGroupPrefix, cancellationToken);
        foreach (var tag in runtimeTags)
        {
            tagsToInvalidate.Add(tag);
        }

        foreach (var tag in tagsToInvalidate)
        {
            await invalidator.InvalidateTagAsync(tag, cancellationToken);
        }
    }

    private async ValueTask<IReadOnlyDictionary<string, int>> BuildActiveTagCountsAsync(CancellationToken cancellationToken)
    {
        var tags = await tagIndexStore.ListTagsAsync(null, cancellationToken);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var group in catalogProvider.GetModels().Select(ResolveGroupPrefix).Distinct(StringComparer.Ordinal))
        {
            counts[group] = tags.Count(tag => tag.StartsWith(group, StringComparison.Ordinal));
        }

        return counts;
    }

    private static string ResolveGroupPrefix(CacheModelDescriptor model) =>
        string.IsNullOrWhiteSpace(model.GroupPrefix) ? model.Prefix : model.GroupPrefix;
}

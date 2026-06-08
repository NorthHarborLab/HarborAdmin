using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;

namespace HarborAdmin.Modules.Admin.Application.Services.System;

/// <summary>
/// 缓存运维管理服务。
/// </summary>
public sealed class CacheManagementService(IHarborCacheManager cacheManager)
{
    /// <summary>
    /// 获取缓存概览。
    /// </summary>
    public async Task<CacheOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var provider = cacheManager.GetProviderInfo();
        var groups = await cacheManager.GetGroupsAsync(cancellationToken);
        return new CacheOverviewDto(
            new CacheProviderInfoDto(provider.Provider, provider.KeyPrefix),
            groups.Select(MapGroup).ToArray());
    }

    /// <summary>
    /// 获取分组下运行时 tag 列表。
    /// </summary>
    public async Task<IReadOnlyList<CacheTagDto>> GetGroupTagsAsync(string groupPrefix, CancellationToken cancellationToken = default)
    {
        var tags = await cacheManager.GetActiveTagsAsync(groupPrefix, cancellationToken);
        return tags.Select(tag => new CacheTagDto(tag.Tag, tag.KeyCount)).ToArray();
    }

    /// <summary>
    /// 获取 tag 下 key 列表。
    /// </summary>
    public async Task<IReadOnlyList<string>> GetTagKeysAsync(string tag, CancellationToken cancellationToken = default) =>
        await cacheManager.GetKeysByTagAsync(tag, cancellationToken);

    /// <summary>
    /// 查看 key 缓存内容。
    /// </summary>
    public async Task<CacheEntryValueDto> GetKeyValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = await cacheManager.GetEntryContentAsync(key, cancellationToken);
        return new CacheEntryValueDto(
            entry.Key,
            entry.Found,
            entry.ModelTypeName,
            entry.Json,
            entry.SizeBytes,
            entry.Truncated);
    }

    /// <summary>
    /// 清理 tag。
    /// </summary>
    public ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default) =>
        cacheManager.InvalidateTagAsync(tag, cancellationToken);

    /// <summary>
    /// 清理 key。
    /// </summary>
    public ValueTask InvalidateKeyAsync(string key, CancellationToken cancellationToken = default) =>
        cacheManager.InvalidateKeyAsync(key, cancellationToken);

    /// <summary>
    /// 清理整组。
    /// </summary>
    public ValueTask InvalidateGroupAsync(string groupPrefix, CancellationToken cancellationToken = default) =>
        cacheManager.InvalidateGroupAsync(groupPrefix, cancellationToken);

    private static CacheGroupSummaryDto MapGroup(CacheGroupDescriptor group)
    {
        var models = group.Models
            .Select(model => new CacheModelSummaryDto(
                model.ModelTypeName,
                model.DisplayName,
                model.Prefix,
                model.KeyTemplate,
                model.ExpirationSeconds,
                model.TagTemplates,
                model.SupportsBulkClear))
            .ToArray();

        return new CacheGroupSummaryDto(
            group.GroupPrefix,
            group.DisplayName,
            group.Module,
            models.Length,
            group.ActiveTagCount,
            models.Any(model => model.SupportsBulkClear),
            models);
    }
}

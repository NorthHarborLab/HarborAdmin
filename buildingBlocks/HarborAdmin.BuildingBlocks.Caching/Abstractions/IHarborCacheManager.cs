namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// Harbor 缓存运维管理入口。
/// </summary>
public interface IHarborCacheManager
{
    /// <summary>
    /// 获取 Provider 信息。
    /// </summary>
    CacheProviderInfo GetProviderInfo();

    /// <summary>
    /// 获取缓存分组概览。
    /// </summary>
    ValueTask<IReadOnlyList<CacheGroupDescriptor>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取运行时 tag 列表。
    /// </summary>
    ValueTask<IReadOnlyList<CacheTagRuntimeInfo>> GetActiveTagsAsync(string? groupPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 tag 关联的 key 列表。
    /// </summary>
    ValueTask<IReadOnlyList<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取 key 对应缓存内容。
    /// </summary>
    ValueTask<CacheEntryContent> GetEntryContentAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 tag 失效缓存。
    /// </summary>
    ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 key 失效缓存。
    /// </summary>
    ValueTask InvalidateKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按分组失效缓存。
    /// </summary>
    ValueTask InvalidateGroupAsync(string groupPrefix, CancellationToken cancellationToken = default);
}

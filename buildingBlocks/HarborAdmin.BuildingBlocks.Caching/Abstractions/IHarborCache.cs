namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// Harbor 对象缓存抽象。
/// </summary>
public interface IHarborCache
{
    /// <summary>
    /// 获取缓存值。
    /// </summary>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取或创建缓存值。
    /// </summary>
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置缓存值。
    /// </summary>
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定 key。
    /// </summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除绑定到指定 tag 的所有 key。
    /// </summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取强类型缓存模型入口。
    /// </summary>
    IHarborCacheSet<TModel> Get<TModel>() where TModel : class;

    /// <summary>
    /// 读取缓存原始 JSON 内容（运维专用）。
    /// </summary>
    ValueTask<CacheRawEntry?> TryGetRawEntryAsync(string key, CancellationToken cancellationToken = default);
}
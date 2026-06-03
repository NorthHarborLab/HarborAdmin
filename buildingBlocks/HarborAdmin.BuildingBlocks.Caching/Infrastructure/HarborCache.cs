using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Options;
using HarborAdmin.BuildingBlocks.Caching.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// Harbor 对象缓存实现。
/// Memory 作为本地一级缓存，Redis/Garnet 作为可选分布式二级缓存。
/// </summary>
internal sealed class HarborCache(
    IMemoryCache memoryCache,
    ITagIndexStore tagIndexStore,
    IOptions<HarborCacheOptions> options,
    IDistributedCache? distributedCache = null) : IHarborCache
{
    /// <summary>
    /// 获取缓存值。
    /// </summary>
    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue<T>(key, out var cached))
        {
            // 本地命中直接返回，避免每次都访问远端 Redis/Garnet。
            return cached;
        }

        if (distributedCache is null)
        {
            // Memory Provider 没有二级缓存，本地未命中即视为不存在。
            return default;
        }

        var bytes = await distributedCache.GetAsync(key, cancellationToken);
        if (bytes is null)
        {
            return default;
        }

        var value = HarborCacheSerializer.Deserialize<T>(bytes);
        if (value is not null)
        {
            // 二级缓存命中后回填 Memory，形成 read-through 的本地热点缓存。
            memoryCache.Set(key, value, GetDefaultExpiration());
        }

        return value;
    }

    /// <summary>
    /// 获取或创建缓存值。
    /// </summary>
    public async ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, tags, cancellationToken);
        return value;
    }

    /// <summary>
    /// 设置缓存值。
    /// </summary>
    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveExpiration = expiration ?? GetDefaultExpiration();
        // Memory 始终写入，即使 Provider 是 Redis/Garnet，也保留进程内热点缓存。
        memoryCache.Set(key, value, effectiveExpiration);

        if (distributedCache is not null)
        {
            await distributedCache.SetAsync(
                key,
                HarborCacheSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = effectiveExpiration },
                cancellationToken);
        }

        if (tags is { Count: > 0 })
        {
            // Tag 索引用于反向查找 key，是 RemoveByTagAsync 能工作的关键。
            await tagIndexStore.BindAsync(key, tags, effectiveExpiration, cancellationToken);
        }
    }

    /// <summary>
    /// 删除指定 key。
    /// </summary>
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        if (distributedCache is not null)
        {
            await distributedCache.RemoveAsync(key, cancellationToken);
        }

        // 删除 key 时同步清理 key -> tags 关系，避免后续 tag 失效扫描到脏引用。
        await tagIndexStore.RemoveKeyAsync(key, cancellationToken);
    }

    /// <summary>
    /// 删除绑定到指定 tag 的所有 key。
    /// </summary>
    public async ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var keys = await tagIndexStore.GetKeysAsync(tag, cancellationToken);
        foreach (var key in keys)
        {
            // tag 失效需要同时清理一级和二级缓存；索引清理放在循环后统一完成。
            memoryCache.Remove(key);
            if (distributedCache is not null)
            {
                await distributedCache.RemoveAsync(key, cancellationToken);
            }
        }

        // 删除 tag 集合及 key -> tag 反向引用，保证下一次失效不会重复处理旧 key。
        await tagIndexStore.RemoveTagAsync(tag, cancellationToken);
    }

    /// <summary>
    /// 获取强类型缓存模型入口。
    /// </summary>
    public IHarborCacheSet<TModel> Get<TModel>() where TModel : class => new HarborCacheSet<TModel>(this);

    /// <summary>
    /// 获取全局默认缓存过期时间。
    /// </summary>
    private TimeSpan GetDefaultExpiration() => TimeSpan.FromSeconds(Math.Max(1, options.Value.DefaultExpirationSeconds));
}
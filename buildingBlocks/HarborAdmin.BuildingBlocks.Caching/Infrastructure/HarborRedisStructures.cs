using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Internal;
using HarborAdmin.BuildingBlocks.Caching.Serialization;
using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// Redis 强类型结构入口实现。
/// Redis 原生结构的强类型门面，统一通过 Attribute 模型生成 Redis key。
/// </summary>
internal sealed class HarborRedisStructures(IHarborRedisClient redisClient, CacheKeyNormalizer keyNormalizer) : IHarborRedisStructures
{
    /// <summary>
    /// 获取 Hash 结构操作入口。
    /// </summary>
    public IHarborRedisHash<TModel> Hash<TModel>(TModel keyModel) where TModel : class =>
        new HarborRedisHash<TModel>(redisClient, BuildKey(keyModel, RedisStructureKind.Hash));

    /// <summary>
    /// 获取 List 结构操作入口。
    /// </summary>
    public IHarborRedisList<TItem> List<TItem>(TItem keyModel) where TItem : class =>
        new HarborRedisList<TItem>(redisClient, BuildKey(keyModel, RedisStructureKind.List));

    /// <summary>
    /// 获取 Counter 结构操作入口。
    /// </summary>
    public IHarborRedisCounter<TCounter> Counter<TCounter>(TCounter keyModel) where TCounter : class =>
        new HarborRedisCounter<TCounter>(redisClient, BuildKey(keyModel, RedisStructureKind.Counter));

    /// <summary>
    /// 根据 key 模型构建 Redis 结构 key。
    /// </summary>
    private string BuildKey<TModel>(TModel keyModel, RedisStructureKind kind) where TModel : class
    {
        // keyModel 只用于提供模板变量，不代表写入 Redis 的 value 类型。
        var key = RedisStructureMetadata.For(typeof(TModel), kind).BuildKey(keyModel);
        return keyNormalizer.ApplyPrefix(key);
    }
}

/// <summary>
/// Redis Hash 结构操作实现。
/// </summary>
internal sealed class HarborRedisHash<TModel>(IHarborRedisClient redisClient, string key) : IHarborRedisHash<TModel> where TModel : class
{
    private readonly IDatabase _database = redisClient.GetDatabase();

    /// <summary>
    /// 设置单个 Hash 字段。
    /// </summary>
    public async ValueTask HSetAsync<TValue>(RedisValue field, TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Hash 字段值统一 JSON 序列化，保证复杂对象和基础类型走同一套格式。
        await _database.HashSetAsync(key, field, HarborCacheSerializer.SerializeToString(value));
    }

    /// <summary>
    /// 获取单个 Hash 字段。
    /// </summary>
    public async ValueTask<TValue?> HGetAsync<TValue>(RedisValue field, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _database.HashGetAsync(key, field);
        // RedisValue.Empty/Null 都按未命中处理，避免反序列化空字符串。
        return value.HasValue ? HarborCacheSerializer.DeserializeFromString<TValue>(value!) : default;
    }

    /// <summary>
    /// 批量设置 Hash 字段。
    /// </summary>
    public async ValueTask HMSetAsync<TValue>(IReadOnlyDictionary<RedisValue, TValue> values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // StackExchange.Redis 的批量 HashSet 需要 HashEntry[]，这里集中完成序列化转换。
        var entries = values
            .Select(pair => new HashEntry(pair.Key, HarborCacheSerializer.SerializeToString(pair.Value)))
            .ToArray();
        await _database.HashSetAsync(key, entries);
    }

    /// <summary>
    /// 批量获取 Hash 字段。
    /// </summary>
    public async ValueTask<IReadOnlyList<TValue?>> HMGetAsync<TValue>(IReadOnlyList<RedisValue> fields, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var values = await _database.HashGetAsync(key, fields.Select(field => field).ToArray());
        return values
            // HMGET 会为缺失字段返回空 RedisValue，保留位置并转换成 default。
            .Select(value => value.HasValue ? HarborCacheSerializer.DeserializeFromString<TValue>(value!) : default)
            .ToArray();
    }

    /// <summary>
    /// 删除单个 Hash 字段。
    /// </summary>
    public async ValueTask HDelAsync(RedisValue field, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.HashDeleteAsync(key, field);
    }
}

/// <summary>
/// Redis List 结构操作实现。
/// </summary>
internal sealed class HarborRedisList<TItem>(IHarborRedisClient redisClient, string key) : IHarborRedisList<TItem> where TItem : class
{
    private readonly IDatabase _database = redisClient.GetDatabase();

    /// <summary>
    /// 从 List 左侧压入元素。
    /// </summary>
    public async ValueTask LPushAsync<TValue>(TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // List 仅规定队列方向，元素格式仍和对象缓存一致使用 JSON。
        await _database.ListLeftPushAsync(key, HarborCacheSerializer.SerializeToString(value));
    }

    /// <summary>
    /// 从 List 右侧压入元素。
    /// </summary>
    public async ValueTask RPushAsync<TValue>(TValue value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.ListRightPushAsync(key, HarborCacheSerializer.SerializeToString(value));
    }

    /// <summary>
    /// 从 List 左侧弹出元素。
    /// </summary>
    public async ValueTask<TValue?> LPopAsync<TValue>(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _database.ListLeftPopAsync(key);
        // 空列表返回空 RedisValue，对调用方表现为 default。
        return value.HasValue ? HarborCacheSerializer.DeserializeFromString<TValue>(value!) : default;
    }

    /// <summary>
    /// 从 List 右侧弹出元素。
    /// </summary>
    public async ValueTask<TValue?> RPopAsync<TValue>(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _database.ListRightPopAsync(key);
        return value.HasValue ? HarborCacheSerializer.DeserializeFromString<TValue>(value!) : default;
    }
}

/// <summary>
/// Redis Counter 结构操作实现。
/// </summary>
internal sealed class HarborRedisCounter<TCounter>(IHarborRedisClient redisClient, string key) : IHarborRedisCounter<TCounter> where TCounter : class
{
    private readonly IDatabase _database = redisClient.GetDatabase();

    /// <summary>
    /// 原子递增 Counter。
    /// </summary>
    public async ValueTask<long> IncrementAsync(long value = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Counter 使用 Redis 原子自增，适合统计、限流等需要并发安全的场景。
        return await _database.StringIncrementAsync(key, value);
    }

    /// <summary>
    /// 原子递减 Counter。
    /// </summary>
    public async ValueTask<long> DecrementAsync(long value = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.StringDecrementAsync(key, value);
    }

    /// <summary>
    /// 设置 Counter 过期时间。
    /// </summary>
    public async ValueTask ExpireAsync(TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.KeyExpireAsync(key, expiration);
    }
}
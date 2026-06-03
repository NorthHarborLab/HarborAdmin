using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 强类型 Redis 结构入口。
/// </summary>
public interface IHarborRedisStructures
{
    /// <summary>
    /// 获取 Hash 结构操作入口。
    /// </summary>
    IHarborRedisHash<TModel> Hash<TModel>(TModel keyModel) where TModel : class;

    /// <summary>
    /// 获取 List 结构操作入口。
    /// </summary>
    IHarborRedisList<TItem> List<TItem>(TItem keyModel) where TItem : class;

    /// <summary>
    /// 获取 Counter 结构操作入口。
    /// </summary>
    IHarborRedisCounter<TCounter> Counter<TCounter>(TCounter keyModel) where TCounter : class;
}

/// <summary>
/// Redis Hash 操作。
/// </summary>
public interface IHarborRedisHash<TModel> where TModel : class
{
    /// <summary>
    /// 设置单个字段。
    /// </summary>
    ValueTask HSetAsync<TValue>(RedisValue field, TValue value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个字段。
    /// </summary>
    ValueTask<TValue?> HGetAsync<TValue>(RedisValue field, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量设置字段。
    /// </summary>
    ValueTask HMSetAsync<TValue>(IReadOnlyDictionary<RedisValue, TValue> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取字段。
    /// </summary>
    ValueTask<IReadOnlyList<TValue?>> HMGetAsync<TValue>(IReadOnlyList<RedisValue> fields, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字段。
    /// </summary>
    ValueTask HDelAsync(RedisValue field, CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis List 操作。
/// </summary>
public interface IHarborRedisList<TItem> where TItem : class
{
    /// <summary>
    /// 从左侧压入。
    /// </summary>
    ValueTask LPushAsync<TValue>(TValue value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从右侧压入。
    /// </summary>
    ValueTask RPushAsync<TValue>(TValue value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从左侧弹出。
    /// </summary>
    ValueTask<TValue?> LPopAsync<TValue>(CancellationToken cancellationToken = default);

    /// <summary>
    /// 从右侧弹出。
    /// </summary>
    ValueTask<TValue?> RPopAsync<TValue>(CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis Counter 操作。
/// </summary>
public interface IHarborRedisCounter<TCounter> where TCounter : class
{
    /// <summary>
    /// 递增。
    /// </summary>
    ValueTask<long> IncrementAsync(long value = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递减。
    /// </summary>
    ValueTask<long> DecrementAsync(long value = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置过期时间。
    /// </summary>
    ValueTask ExpireAsync(TimeSpan expiration, CancellationToken cancellationToken = default);
}

using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// Redis 结构在 Memory Provider 下的不可用实现。
/// </summary>
internal sealed class UnavailableRedisStructures : IHarborRedisStructures
{
    /// <summary>
    /// 获取不可用的 Hash 结构操作入口。
    /// </summary>
    public IHarborRedisHash<TModel> Hash<TModel>(TModel keyModel) where TModel : class => new UnavailableRedisHash<TModel>();

    /// <summary>
    /// 获取不可用的 List 结构操作入口。
    /// </summary>
    public IHarborRedisList<TItem> List<TItem>(TItem keyModel) where TItem : class => new UnavailableRedisList<TItem>();

    /// <summary>
    /// 获取不可用的 Counter 结构操作入口。
    /// </summary>
    public IHarborRedisCounter<TCounter> Counter<TCounter>(TCounter keyModel) where TCounter : class => new UnavailableRedisCounter<TCounter>();

    /// <summary>
    /// 创建 Redis 结构不可用异常。
    /// </summary>
    private static InvalidOperationException Error() =>
        new("Redis typed structures require Harbor:Cache:Provider to be Redis or Garnet.");

    /// <summary>
    /// 不可用的 Redis Hash 操作。
    /// </summary>
    private sealed class UnavailableRedisHash<TModel> : IHarborRedisHash<TModel> where TModel : class
    {
        /// <summary>
        /// 抛出 Redis Hash 设置不可用异常。
        /// </summary>
        public ValueTask HSetAsync<TValue>(RedisValue field, TValue value, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis Hash 获取不可用异常。
        /// </summary>
        public ValueTask<TValue?> HGetAsync<TValue>(RedisValue field, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis Hash 批量设置不可用异常。
        /// </summary>
        public ValueTask HMSetAsync<TValue>(IReadOnlyDictionary<RedisValue, TValue> values, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis Hash 批量获取不可用异常。
        /// </summary>
        public ValueTask<IReadOnlyList<TValue?>> HMGetAsync<TValue>(IReadOnlyList<RedisValue> fields, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis Hash 删除不可用异常。
        /// </summary>
        public ValueTask HDelAsync(RedisValue field, CancellationToken cancellationToken = default) => throw Error();
    }

    /// <summary>
    /// 不可用的 Redis List 操作。
    /// </summary>
    private sealed class UnavailableRedisList<TItem> : IHarborRedisList<TItem> where TItem : class
    {
        /// <summary>
        /// 抛出 Redis List 左侧压入不可用异常。
        /// </summary>
        public ValueTask LPushAsync<TValue>(TValue value, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis List 右侧压入不可用异常。
        /// </summary>
        public ValueTask RPushAsync<TValue>(TValue value, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis List 左侧弹出不可用异常。
        /// </summary>
        public ValueTask<TValue?> LPopAsync<TValue>(CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis List 右侧弹出不可用异常。
        /// </summary>
        public ValueTask<TValue?> RPopAsync<TValue>(CancellationToken cancellationToken = default) => throw Error();
    }

    /// <summary>
    /// 不可用的 Redis Counter 操作。
    /// </summary>
    private sealed class UnavailableRedisCounter<TCounter> : IHarborRedisCounter<TCounter> where TCounter : class
    {
        /// <summary>
        /// 抛出 Redis Counter 递增不可用异常。
        /// </summary>
        public ValueTask<long> IncrementAsync(long value = 1, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis Counter 递减不可用异常。
        /// </summary>
        public ValueTask<long> DecrementAsync(long value = 1, CancellationToken cancellationToken = default) => throw Error();

        /// <summary>
        /// 抛出 Redis Counter 设置过期不可用异常。
        /// </summary>
        public ValueTask ExpireAsync(TimeSpan expiration, CancellationToken cancellationToken = default) => throw Error();
    }
}

using System.Linq.Expressions;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Internal;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// 强类型缓存集合实现。
/// 强类型缓存入口，把 Where 表达式转换成 CacheModelMetadata 定义的最终 key。
/// </summary>
internal sealed class HarborCacheSet<TModel>(IHarborCache cache) : IHarborCacheSet<TModel> where TModel : class
{
    /// <summary>
    /// 根据等值表达式创建缓存查询。
    /// </summary>
    public IHarborCacheQuery<TModel> Where(Expression<Func<TModel, bool>> predicate)
    {
        var metadata = CacheModelMetadata.For(typeof(TModel));
        // 这里只解析 [CacheKeyPart] 字段的等值条件，不承担通用 LINQ 查询职责。
        var keyParts = ExpressionKeyParser.Parse(predicate);
        var key = metadata.BuildKey(keyParts);
        return new HarborCacheQuery<TModel>(cache, metadata, key);
    }
}

/// <summary>
/// 强类型缓存查询实现。
/// </summary>
internal sealed class HarborCacheQuery<TModel>(IHarborCache cache, CacheModelMetadata metadata, string key) : IHarborCacheQuery<TModel> where TModel : class
{
    /// <summary>
    /// 获取缓存模型。
    /// </summary>
    public ValueTask<TModel?> GetAsync(CancellationToken cancellationToken = default) =>
        cache.GetAsync<TModel>(key, cancellationToken);

    /// <summary>
    /// 获取或创建缓存模型。
    /// </summary>
    public async ValueTask<TModel> GetOrCreateAsync(
        Func<CancellationToken, ValueTask<TModel>> factory,
        CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<TModel>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        // 写入时从模型实例生成 Tag，这样显式失效和数据库事件失效都能按 tag 找回 key。
        await cache.SetAsync(key, value, metadata.Expiration, metadata.BuildTags(value), cancellationToken);
        return value;
    }

    /// <summary>
    /// 删除当前查询对应的缓存。
    /// </summary>
    public ValueTask RemoveAsync(CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    /// <summary>
    /// 写入当前查询对应的缓存值。
    /// </summary>
    public ValueTask SetAsync(TModel value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) =>
        cache.SetAsync(key, value, expiration ?? metadata.Expiration, metadata.BuildTags(value), cancellationToken);
}
using System.Linq.Expressions;

namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 强类型缓存模型集合入口。
/// </summary>
public interface IHarborCacheSet<TModel> where TModel : class
{
    /// <summary>
    /// 指定用于解析缓存 key 的等值条件。
    /// </summary>
    IHarborCacheQuery<TModel> Where(Expression<Func<TModel, bool>> predicate);
}

/// <summary>
/// 强类型缓存查询。
/// </summary>
public interface IHarborCacheQuery<TModel> where TModel : class
{
    /// <summary>
    /// 获取缓存值。
    /// </summary>
    ValueTask<TModel?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取或创建缓存值。
    /// </summary>
    ValueTask<TModel> GetOrCreateAsync(
        Func<CancellationToken, ValueTask<TModel>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除缓存值。
    /// </summary>
    ValueTask RemoveAsync(CancellationToken cancellationToken = default);
}

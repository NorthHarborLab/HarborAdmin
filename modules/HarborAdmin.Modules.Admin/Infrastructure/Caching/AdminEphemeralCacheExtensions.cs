using System.Linq.Expressions;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;

namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// Admin 模块 ephemeral 强类型缓存扩展。
/// </summary>
public static class AdminEphemeralCacheExtensions
{
    /// <summary>
    /// 按强类型模型条件写入缓存。
    /// </summary>
    public static ValueTask SetAsync<TModel>(
        this IHarborCache cache,
        Expression<Func<TModel, bool>> predicate,
        TModel value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where TModel : class =>
        cache.Get<TModel>().Where(predicate).SetAsync(value, expiration, cancellationToken);

    /// <summary>
    /// 按强类型模型条件读取并删除缓存项，用于一次性消费场景。
    /// </summary>
    public static async ValueTask<TModel?> TryConsumeAsync<TModel>(
        this IHarborCache cache,
        Expression<Func<TModel, bool>> predicate,
        CancellationToken cancellationToken = default) where TModel : class
    {
        var query = cache.Get<TModel>().Where(predicate);
        var value = await query.GetAsync(cancellationToken);
        if (value is null)
        {
            return null;
        }

        await query.RemoveAsync(cancellationToken);
        return value;
    }
}

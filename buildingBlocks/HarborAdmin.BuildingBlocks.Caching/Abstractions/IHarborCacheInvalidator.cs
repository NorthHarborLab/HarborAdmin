namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 缓存失效服务。
/// </summary>
public interface IHarborCacheInvalidator
{
    /// <summary>
    /// 删除指定 key。
    /// </summary>
    ValueTask InvalidateKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定 tag 关联的所有缓存。
    /// </summary>
    ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据实体变更触发缓存失效。
    /// </summary>
    ValueTask InvalidateEntityAsync(object entity, string operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// 供数据基础设施调用的实体缓存失效入口。
/// </summary>
public interface IHarborEntityCacheInvalidator
{
    /// <summary>
    /// 根据实体变更触发缓存失效。
    /// </summary>
    ValueTask InvalidateEntityAsync(object entity, string operation, CancellationToken cancellationToken = default);
}

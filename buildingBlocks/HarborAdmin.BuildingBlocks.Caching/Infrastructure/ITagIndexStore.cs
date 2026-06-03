namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// 缓存 tag 与 key 的索引存储。
/// </summary>
internal interface ITagIndexStore
{
    /// <summary>
    /// 绑定缓存 key 与 tag 集合。
    /// </summary>
    ValueTask BindAsync(string key, IReadOnlyCollection<string> tags, TimeSpan? expiration, CancellationToken cancellationToken);

    /// <summary>
    /// 获取指定 tag 关联的缓存 key。
    /// </summary>
    ValueTask<IReadOnlyList<string>> GetKeysAsync(string tag, CancellationToken cancellationToken);

    /// <summary>
    /// 移除指定缓存 key 的索引关系。
    /// </summary>
    ValueTask RemoveKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// 移除指定 tag 的索引关系。
    /// </summary>
    ValueTask RemoveTagAsync(string tag, CancellationToken cancellationToken);
}

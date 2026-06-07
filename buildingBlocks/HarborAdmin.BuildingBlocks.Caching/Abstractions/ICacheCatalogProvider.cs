namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 缓存模型目录提供器。
/// </summary>
public interface ICacheCatalogProvider
{
    /// <summary>
    /// 获取全部缓存模型描述。
    /// </summary>
    IReadOnlyList<CacheModelDescriptor> GetModels();

    /// <summary>
    /// 获取按 prefix 聚合的分组。
    /// </summary>
    IReadOnlyList<CacheGroupDescriptor> BuildGroups(IReadOnlyDictionary<string, int> activeTagCountsByPrefix);

    /// <summary>
    /// 根据 key 匹配所属模型。
    /// </summary>
    CacheModelDescriptor? MatchModelByKey(string key);
}

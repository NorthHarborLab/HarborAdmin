using HarborAdmin.BuildingBlocks.Caching.Attributes;
using HarborAdmin.Modules.International.Domain.Entities;
using HarborAdmin.Modules.International.Contracts.Page.Dto;

namespace HarborAdmin.Modules.International.Infrastructure.Caching;

/// <summary>
/// 国际化单页面资源包缓存模型。
/// </summary>
[CacheCatalog("国际化页面资源包", GroupPrefix = "harbor:international", GroupName = "国际化", Module = "International", Order = 12)]
[CacheKey("harbor:international:page", Key = "{Path}", ExpirationSeconds = 600)]
[CacheTag(InternationalCacheKeys.AllTag)]
[CacheTag(InternationalCacheKeys.PageTagTemplate, typeof(InternationalPage))]
[CacheTag(InternationalCacheKeys.PageIdTagTemplate, typeof(InternationalEntry))]
public sealed class InternationalPageBundleCacheModel
{
    /// <summary>
    /// 页面完整路径。
    /// </summary>
    [CacheKeyPart]
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 页面 ID，用于条目变更按页面 ID 失效。
    /// </summary>
    public long PageId { get; init; }

    /// <summary>
    /// 资源包 DTO。
    /// </summary>
    public InternationalPageBundleDto Value { get; init; } = new(string.Empty, 0, new Dictionary<string, object>());
}

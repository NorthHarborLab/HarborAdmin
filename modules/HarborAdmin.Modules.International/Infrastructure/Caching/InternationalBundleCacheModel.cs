using HarborAdmin.BuildingBlocks.Caching.Attributes;
using HarborAdmin.Modules.International.Domain.Entities;
using HarborAdmin.Modules.International.Contracts.Resource.Dto;

namespace HarborAdmin.Modules.International.Infrastructure.Caching;

/// <summary>
/// 国际化全量资源包缓存模型。
/// </summary>
[CacheCatalog("国际化全量资源包", GroupPrefix = "harbor:international", GroupName = "国际化", Module = "International", Order = 11)]
[CacheKey("harbor:international", Key = "{Id}", ExpirationSeconds = 600)]
[CacheTag(InternationalCacheKeys.AllTag, typeof(InternationalPage), typeof(InternationalEntry), typeof(InternationalEntryTranslation))]
public sealed class InternationalBundleCacheModel
{
    /// <summary>
    /// 固定缓存主键。
    /// </summary>
    [CacheKeyPart]
    public string Id { get; init; } = InternationalCacheKeys.BundleKey;

    /// <summary>
    /// 资源包 DTO。
    /// </summary>
    public InternationalBundleDto Value { get; init; } = new(0, new Dictionary<string, object>());
}

using HarborAdmin.BuildingBlocks.Caching.Attributes;
using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Infrastructure.Caching;

/// <summary>
/// 国际化版本缓存模型。
/// </summary>
[CacheCatalog("国际化版本", GroupPrefix = "harbor:international", GroupName = "国际化", Module = "International", Order = 10)]
[CacheKey("harbor:international", Key = "{Id}", ExpirationSeconds = 600)]
[CacheTag(InternationalCacheKeys.AllTag, typeof(InternationalPage))]
public sealed class InternationalVersionCacheModel
{
    /// <summary>
    /// 固定缓存主键。
    /// </summary>
    [CacheKeyPart]
    public string Id { get; init; } = InternationalCacheKeys.VersionKey;

    /// <summary>
    /// 版本 DTO。
    /// </summary>
    public InternationalVersionDto Value { get; init; } = new(0, []);
}

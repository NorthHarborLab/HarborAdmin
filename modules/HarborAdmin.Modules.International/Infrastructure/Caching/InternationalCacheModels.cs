using HarborAdmin.BuildingBlocks.Caching.Attributes;
using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Contracts.Requests;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Infrastructure.Caching;

/// <summary>
/// 国际化版本缓存模型。
/// </summary>
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

/// <summary>
/// 国际化全量资源包缓存模型。
/// </summary>
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

/// <summary>
/// 国际化单页面资源包缓存模型。
/// </summary>
[CacheKey("harbor:international:page", Key = "{PageKey}", ExpirationSeconds = 600)]
[CacheTag(InternationalCacheKeys.AllTag)]
[CacheTag(InternationalCacheKeys.PageTagTemplate, typeof(InternationalPage))]
[CacheTag(InternationalCacheKeys.PageIdTagTemplate, typeof(InternationalEntry))]
public sealed class InternationalPageBundleCacheModel
{
    /// <summary>
    /// 页面主键。
    /// </summary>
    [CacheKeyPart]
    public string PageKey { get; init; } = string.Empty;

    /// <summary>
    /// 页面 ID，用于条目变更按页面 ID 失效。
    /// </summary>
    public long PageId { get; init; }

    /// <summary>
    /// 资源包 DTO。
    /// </summary>
    public InternationalPageBundleDto Value { get; init; } = new(string.Empty, 0, new Dictionary<string, object>());
}

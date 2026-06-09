using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.Modules.International.Infrastructure.Caching;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化缓存失效协调器。
/// </summary>
public sealed class InternationalCacheCoordinator(IHarborCacheInvalidator cacheInvalidator)
{
    /// <summary>
    /// 失效国际化模块的全局缓存。
    /// </summary>
    public async Task InvalidateAllAsync(CancellationToken cancellationToken) =>
        await cacheInvalidator.InvalidateTagAsync(InternationalCacheKeys.AllTag, cancellationToken);

    /// <summary>
    /// 失效指定页面相关的缓存。
    /// </summary>
    public async Task InvalidatePageAsync(long pageId, string path, CancellationToken cancellationToken)
    {
        await InvalidateAllAsync(cancellationToken);
        await cacheInvalidator.InvalidateTagAsync(InternationalCacheKeys.PageIdTag(pageId), cancellationToken);
        await cacheInvalidator.InvalidateTagAsync(InternationalCacheKeys.PageTag(path), cancellationToken);
    }
}

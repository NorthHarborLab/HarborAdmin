using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Infrastructure.Caching;
using HarborAdmin.Modules.International.Contracts.Page.Dto;
using HarborAdmin.Modules.International.Contracts.Resource.Dto;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化资源包服务。
/// </summary>
public sealed class InternationalResourceBundleService(
    IInternationalPageRepository pageRepository,
    IInternationalVersionRepository versionRepository,
    IHarborCache cache)
{
    /// <summary>
    /// 获取当前国际化版本。
    /// </summary>
    public async Task<InternationalVersionDto> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var model = await cache.Get<InternationalVersionCacheModel>()
            .Where(model => model.Id == InternationalCacheKeys.VersionKey)
            .GetOrCreateAsync(async ct =>
            {
                var version = await versionRepository.GetVersionAsync(ct);
                var pages = await versionRepository.ListPageVersionsAsync(ct);
                var pageVersions = pages
                    .Select(page => new InternationalPageVersionDto(page.FullPath, page.Version))
                    .ToList();
                return new InternationalVersionCacheModel
                {
                    Value = new InternationalVersionDto(version, pageVersions)
                };
            }, cancellationToken);
        return model.Value;
    }

    /// <summary>
    /// 获取前端可直接合并的国际化资源包。
    /// </summary>
    public async Task<InternationalBundleDto> GetBundleAsync(CancellationToken cancellationToken = default)
    {
        var model = await cache.Get<InternationalBundleCacheModel>()
            .Where(model => model.Id == InternationalCacheKeys.BundleKey)
            .GetOrCreateAsync(async ct =>
            {
                var pages = await pageRepository.ListPagesWithEntriesAsync(ct);
                var messages = new Dictionary<string, object>(StringComparer.Ordinal);

                foreach (var page in pages)
                {
                    InternationalBundleBuilder.MergePageMessages(messages, page);
                }

                var version = await versionRepository.GetVersionAsync(ct);
                return new InternationalBundleCacheModel
                {
                    Value = new InternationalBundleDto(version, messages)
                };
            }, cancellationToken);
        return model.Value;
    }

    /// <summary>
    /// 获取前端可直接合并的单页面国际化资源包。
    /// </summary>
    public async Task<InternationalPageBundleDto> GetPageBundleAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.Trim();
        var model = await cache.Get<InternationalPageBundleCacheModel>()
            .Where(model => model.Path == path)
            .GetOrCreateAsync(async ct =>
            {
                var page = await pageRepository.GetPageWithEntriesByPathAsync(path, ct)
                           ?? throw new NotFoundDomainException($"国际化页面 '{path}' 不存在。");
                var messages = new Dictionary<string, object>(StringComparer.Ordinal);
                InternationalBundleBuilder.MergePageMessages(messages, page);
                return new InternationalPageBundleCacheModel
                {
                    Path = page.FullPath,
                    PageId = page.Id,
                    Value = new InternationalPageBundleDto(page.FullPath, page.Version, messages)
                };
            }, cancellationToken);
        return model.Value;
    }
}

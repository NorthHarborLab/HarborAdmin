using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Contracts.Page.Dto;
using HarborAdmin.Modules.International.Contracts.Page.Request;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化页面管理服务。
/// </summary>
public sealed class InternationalPageService(
    IInternationalRepository repository,
    InternationalCacheCoordinator cacheCoordinator,
    IHarborMapper mapper)
{
    /// <summary>
    /// 列出页面命名空间。
    /// </summary>
    public async Task<IReadOnlyList<InternationalPageDto>> ListPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await repository.ListPagesAsync(cancellationToken);
        return pages.Select(page => mapper.Map<InternationalPageDto>(page)).ToList();
    }

    /// <summary>
    /// 保存页面命名空间（创建或更新）。
    /// </summary>
    public async Task<InternationalPageDto> SavePageAsync(long? id, SaveInternationalPageRequest request, CancellationToken cancellationToken = default)
    {
        if (id is null)
        {
            return await CreatePageAsync(request, cancellationToken);
        }

        return await UpdatePageAsync(id.Value, request, cancellationToken);
    }

    /// <summary>
    /// 删除页面命名空间及其全部翻译条目。
    /// </summary>
    public async Task DeletePageAsync(long id, CancellationToken cancellationToken = default)
    {
        var page = await RequirePageAsync(id, cancellationToken);
        await repository.DeletePageAsync(id, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
    }

    /// <summary>
    /// 发布页面版本。
    /// </summary>
    public async Task<InternationalPageDto> PublishPageVersionAsync(long pageId, CancellationToken cancellationToken = default)
    {
        await repository.IncreasePageVersionAsync(pageId, cancellationToken);
        var page = await RequirePageAsync(pageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return mapper.Map<InternationalPageDto>(page);
    }

    internal async Task<InternationalPage> RequirePageAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetPageAsync(id, cancellationToken)
        ?? throw new NotFoundDomainException($"国际化页面 '{id}' 不存在。");

    /// <summary>
    /// 创建页面命名空间并失效全局资源缓存。
    /// </summary>
    private async Task<InternationalPageDto> CreatePageAsync(SaveInternationalPageRequest request, CancellationToken cancellationToken)
    {
        var pageKey = request.PageKey.Trim();
        var existing = await repository.GetPageByKeyAsync(pageKey, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictDomainException($"国际化页面 '{pageKey}' 已存在。");
        }

        var page = new InternationalPage
        {
            PageKey = pageKey,
            Version = 0,
            Name = request.Name.Trim(),
            Remark = request.Remark?.Trim()
        };

        var created = await repository.InsertPageAsync(page, cancellationToken);
        // 新页面会改变全量版本和 bundle 列表，需要失效整个国际化缓存域。
        await cacheCoordinator.InvalidateAllAsync(cancellationToken);
        return mapper.Map<InternationalPageDto>(created);
    }

    /// <summary>
    /// 更新页面命名空间并处理页面 key 变更后的缓存失效。
    /// </summary>
    private async Task<InternationalPageDto> UpdatePageAsync(long id, SaveInternationalPageRequest request, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(id, cancellationToken);
        var pageKey = request.PageKey.Trim();
        if (!string.Equals(page.PageKey, pageKey, StringComparison.Ordinal))
        {
            var existing = await repository.GetPageByKeyAsync(pageKey, cancellationToken);
            if (existing is not null && existing.Id != page.Id)
            {
                throw new ConflictDomainException($"国际化页面 '{pageKey}' 已存在。");
            }
        }

        var oldPageKey = page.PageKey;
        page.PageKey = pageKey;
        page.Name = request.Name.Trim();
        page.Remark = request.Remark?.Trim();

        await repository.UpdatePageAsync(page, cancellationToken);
        // pageKey 参与单页面 bundle 缓存 key，改名时旧 key 和新 key 都必须失效。
        await cacheCoordinator.InvalidatePageAsync(page.Id, oldPageKey, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return mapper.Map<InternationalPageDto>(page);
    }
}

using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Contracts.Entry.Dto;
using HarborAdmin.Modules.International.Contracts.Entry.Request;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化条目管理服务。
/// </summary>
public sealed class InternationalEntryService(
    IInternationalEntryRepository repository,
    InternationalPageService pageService,
    InternationalCacheCoordinator cacheCoordinator,
    IHarborMapper mapper)
{
    /// <summary>
    /// 列出页面条目树。
    /// </summary>
    public async Task<IReadOnlyList<InternationalEntryDto>> ListEntriesAsync(long pageId, CancellationToken cancellationToken = default)
    {
        _ = await pageService.RequirePageAsync(pageId, cancellationToken);
        var entries = await repository.ListEntriesAsync(pageId, cancellationToken);
        return BuildEntryTree(entries);
    }

    /// <summary>
    /// 保存页面条目节点（创建或更新）。
    /// </summary>
    public async Task<InternationalEntryDto> SaveEntryAsync(
        SaveInternationalEntryRequest request,
        long? pageId = null,
        long? entryId = null,
        CancellationToken cancellationToken = default)
    {
        if (entryId is null)
        {
            if (pageId is null or <= 0)
            {
                throw new ValidationDomainException("创建条目时必须指定页面 ID。");
            }

            return await CreateEntryAsync(pageId.Value, request, cancellationToken);
        }

        return await UpdateEntryAsync(entryId.Value, request, cancellationToken);
    }

    /// <summary>
    /// 删除页面条目节点及其子节点。
    /// </summary>
    public async Task DeleteEntryAsync(long entryId, CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        await repository.DeleteEntryAsync(entryId, cancellationToken);
        var page = await pageService.RequirePageAsync(entry.PageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
    }

    internal async Task<InternationalEntry> RequireEntryAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetEntryAsync(id, cancellationToken)
        ?? throw new NotFoundDomainException($"国际化条目 '{id}' 不存在。");

    /// <summary>
    /// 创建页面条目并写入初始翻译。
    /// </summary>
    private async Task<InternationalEntryDto> CreateEntryAsync(
        long pageId,
        SaveInternationalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var page = await pageService.RequirePageAsync(pageId, cancellationToken);
        var parentId = await NormalizeParentIdAsync(pageId, request.ParentId, cancellationToken);
        var key = NormalizeEntryKey(request.Key);
        await EnsureSiblingKeyUniqueAsync(pageId, parentId, key, null, cancellationToken);

        var entry = new InternationalEntry
        {
            PageId = pageId,
            ParentId = parentId,
            Key = key,
            Remark = request.Remark?.Trim(),
            SortOrder = request.SortOrder
        };

        var translations = ToTranslations(request.Translations);
        var created = await repository.InsertEntryAsync(entry, translations, cancellationToken);
        // 仓储负责回填 EntryId，这里把翻译挂回聚合用于返回 DTO。
        created.Translations = translations.ToList();
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
        return MapEntryDto(created, []);
    }

    /// <summary>
    /// 更新条目基础信息并整体替换翻译列表。
    /// </summary>
    private async Task<InternationalEntryDto> UpdateEntryAsync(
        long entryId,
        SaveInternationalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        var key = NormalizeEntryKey(request.Key);
        await EnsureSiblingKeyUniqueAsync(entry.PageId, entry.ParentId, key, entry.Id, cancellationToken);

        entry.Key = key;
        entry.Remark = request.Remark?.Trim();
        entry.SortOrder = request.SortOrder;

        var translations = ToTranslations(request.Translations);
        await repository.UpdateEntryAsync(entry, translations, cancellationToken);
        // 翻译列表按请求整体替换，返回值也使用替换后的内存集合。
        entry.Translations = translations.ToList();
        var page = await pageService.RequirePageAsync(entry.PageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
        return MapEntryDto(entry, []);
    }

    /// <summary>
    /// 规范化父级条目并校验父子节点属于同一页面。
    /// </summary>
    private async Task<long?> NormalizeParentIdAsync(long pageId, long? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return null;
        }

        var parent = await RequireEntryAsync(parentId.Value, cancellationToken);
        if (parent.PageId != pageId)
        {
            throw new ValidationDomainException($"父级条目 '{parentId}' 不属于页面 '{pageId}'。");
        }

        return parentId;
    }

    /// <summary>
    /// 确保同一父级下条目键名唯一。
    /// </summary>
    private async Task EnsureSiblingKeyUniqueAsync(
        long pageId,
        long? parentId,
        string key,
        long? ignoredId,
        CancellationToken cancellationToken)
    {
        var siblings = await repository.ListEntriesAsync(pageId, cancellationToken);
        var exists = siblings.Any(entry =>
            entry.ParentId == parentId &&
            entry.Id != ignoredId &&
            string.Equals(entry.Key, key, StringComparison.Ordinal));
        if (exists)
        {
            throw new ConflictDomainException($"同级条目键名 '{key}' 已存在。");
        }
    }

    /// <summary>
    /// 将扁平条目列表组装为前端展示用树。
    /// </summary>
    private IReadOnlyList<InternationalEntryDto> BuildEntryTree(IReadOnlyList<InternationalEntry> entries)
    {
        var groups = entries
            .GroupBy(entry => entry.ParentId)
            .ToDictionary(
                group => group.Key ?? 0,
                group => group.OrderBy(entry => entry.SortOrder).ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList());
        return BuildEntryDtos(groups, 0);
    }

    /// <summary>
    /// 递归构建指定父级下的条目 DTO。
    /// </summary>
    private IReadOnlyList<InternationalEntryDto> BuildEntryDtos(IReadOnlyDictionary<long, List<InternationalEntry>> groups, long parentId)
    {
        if (!groups.TryGetValue(parentId, out var entries))
        {
            return [];
        }

        return entries
            .Select(entry => MapEntryDto(entry, BuildEntryDtos(groups, entry.Id)))
            .ToList();
    }

    /// <summary>
    /// 规范化条目键名并禁止会破坏路径语义的分隔符。
    /// </summary>
    private static string NormalizeEntryKey(string value)
    {
        var normalized = value.Trim();
        if (normalized.Contains('.') || normalized.Contains(':') || normalized.Contains('/'))
        {
            throw new ValidationDomainException("条目键名不能包含 '.'、':' 或 '/'。");
        }

        return normalized;
    }

    /// <summary>
    /// 将请求中的翻译 DTO 转换为实体，并按 locale 去重。
    /// </summary>
    private static IReadOnlyList<InternationalEntryTranslation> ToTranslations(IReadOnlyList<InternationalEntryTranslationDto> translations) =>
        translations
            .Select(item => new InternationalEntryTranslation
            {
                Locale = item.Locale.Trim(),
                Value = item.Value
            })
            .GroupBy(item => item.Locale, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(item => item.Locale, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// 映射条目 DTO 并附加子节点。
    /// </summary>
    private InternationalEntryDto MapEntryDto(InternationalEntry entry, IReadOnlyList<InternationalEntryDto> children) =>
        mapper.Map<InternationalEntryDto>(entry) with { Children = children };
}

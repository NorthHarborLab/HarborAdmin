using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Contracts.Requests;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化条目管理服务。
/// </summary>
public sealed class InternationalEntryService(
    IInternationalRepository repository,
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
    /// 创建页面条目节点。
    /// </summary>
    public async Task<InternationalEntryDto> CreateEntryAsync(
        long pageId,
        CreateInternationalEntryRequest request,
        CancellationToken cancellationToken = default)
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
            SortOrder = request.SortOrder,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var translations = ToTranslations(request.Translations);
        var created = await repository.InsertEntryAsync(entry, translations, cancellationToken);
        created.Translations = translations.ToList();
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return MapEntryDto(created, []);
    }

    /// <summary>
    /// 更新页面条目节点。
    /// </summary>
    public async Task<InternationalEntryDto> UpdateEntryAsync(
        long entryId,
        UpdateInternationalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        var key = NormalizeEntryKey(request.Key);
        await EnsureSiblingKeyUniqueAsync(entry.PageId, entry.ParentId, key, entry.Id, cancellationToken);

        entry.Key = key;
        entry.Remark = request.Remark?.Trim();
        entry.SortOrder = request.SortOrder;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        var translations = ToTranslations(request.Translations);
        await repository.UpdateEntryAsync(entry, translations, cancellationToken);
        entry.Translations = translations.ToList();
        var page = await pageService.RequirePageAsync(entry.PageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return MapEntryDto(entry, []);
    }

    /// <summary>
    /// 删除页面条目节点及其子节点。
    /// </summary>
    public async Task DeleteEntryAsync(long entryId, CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        await repository.DeleteEntryAsync(entryId, cancellationToken);
        var page = await pageService.RequirePageAsync(entry.PageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
    }

    internal async Task<InternationalEntry> RequireEntryAsync(long id, CancellationToken cancellationToken) =>
        await repository.GetEntryAsync(id, cancellationToken)
        ?? throw new NotFoundDomainException($"国际化条目 '{id}' 不存在。");

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

    private IReadOnlyList<InternationalEntryDto> BuildEntryTree(IReadOnlyList<InternationalEntry> entries)
    {
        var groups = entries
            .GroupBy(entry => entry.ParentId)
            .ToDictionary(
                group => group.Key ?? 0,
                group => group.OrderBy(entry => entry.SortOrder).ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList());
        return BuildEntryDtos(groups, 0);
    }

    private IReadOnlyList<InternationalEntryDto> BuildEntryDtos(
        IReadOnlyDictionary<long, List<InternationalEntry>> groups,
        long parentId)
    {
        if (!groups.TryGetValue(parentId, out var entries))
        {
            return [];
        }

        return entries
            .Select(entry => MapEntryDto(entry, BuildEntryDtos(groups, entry.Id)))
            .ToList();
    }

    private static string NormalizeEntryKey(string value)
    {
        var normalized = value.Trim();
        if (normalized.Contains('.') || normalized.Contains(':') || normalized.Contains('/'))
        {
            throw new ValidationDomainException("条目键名不能包含 '.'、':' 或 '/'。");
        }

        return normalized;
    }

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

    private InternationalEntryDto MapEntryDto(InternationalEntry entry, IReadOnlyList<InternationalEntryDto> children) =>
        mapper.Map<InternationalEntryDto>(entry) with { Children = children };
}

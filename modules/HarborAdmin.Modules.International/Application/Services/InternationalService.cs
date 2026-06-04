using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Contracts.Requests;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Domain.Entities;
using HarborAdmin.Modules.International.Infrastructure.Caching;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Mapping;
using System.Text.Json;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 前端国际化管理服务
/// </summary>
public sealed class InternationalService(
    IInternationalRepository repository,
    IHarborCache cache,
    IHarborCacheInvalidator cacheInvalidator,
    IAiClient aiClient,
    IHarborMapper mapper)
{
    /// <summary>
    /// 未命中指定语言时使用的默认语言。
    /// </summary>
    private const string DefaultLocale = "zh-CN";

    /// <summary>
    /// AI 翻译业务 Key。
    /// </summary>
    private const string TranslateBusinessKey = "international.translate";

    /// <summary>
    /// AI 翻译完成回调 Topic。
    /// </summary>
    internal const string TranslationCompletedTopic = "harbor.international.translation.completed.v1";

    /// <summary>
    /// 列出页面命名空间
    /// </summary>
    public async Task<IReadOnlyList<InternationalPageDto>> ListPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await repository.ListPagesAsync(cancellationToken);
        return pages.Select(page => mapper.Map<InternationalPageDto>(page)).ToList();
    }

    /// <summary>
    /// 创建页面命名空间
    /// </summary>
    public async Task<InternationalPageDto> CreatePageAsync(CreateInternationalPageRequest request, CancellationToken cancellationToken = default)
    {
        var pageKey = NormalizeKey(request.PageKey, nameof(request.PageKey));
        var existing = await repository.GetPageByKeyAsync(pageKey, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"International page '{pageKey}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var page = new InternationalPage
        {
            PageKey = pageKey,
            Version = 0,
            Name = NormalizeRequired(request.Name, nameof(request.Name)),
            Remark = request.Remark?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await repository.InsertPageAsync(page, cancellationToken);
        await InvalidateAllAsync(cancellationToken);
        return mapper.Map<InternationalPageDto>(created);
    }

    /// <summary>
    /// 更新页面命名空间
    /// </summary>
    public async Task<InternationalPageDto> UpdatePageAsync(long id, UpdateInternationalPageRequest request, CancellationToken cancellationToken = default)
    {
        var page = await RequirePageAsync(id, cancellationToken);
        var pageKey = NormalizeKey(request.PageKey, nameof(request.PageKey));
        if (!string.Equals(page.PageKey, pageKey, StringComparison.Ordinal))
        {
            var existing = await repository.GetPageByKeyAsync(pageKey, cancellationToken);
            if (existing is not null && existing.Id != page.Id)
            {
                throw new InvalidOperationException($"International page '{pageKey}' already exists.");
            }
        }

        var oldPageKey = page.PageKey;
        page.PageKey = pageKey;
        page.Name = NormalizeRequired(request.Name, nameof(request.Name));
        page.Remark = request.Remark?.Trim();
        page.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdatePageAsync(page, cancellationToken);
        await InvalidatePageAsync(page.Id, oldPageKey, cancellationToken);
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return mapper.Map<InternationalPageDto>(page);
    }

    /// <summary>
    /// 删除页面命名空间及其全部翻译条目
    /// </summary>
    public async Task DeletePageAsync(long id, CancellationToken cancellationToken = default)
    {
        var page = await RequirePageAsync(id, cancellationToken);
        await repository.DeletePageAsync(id, cancellationToken);
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
    }

    /// <summary>
    /// 列出页面条目树
    /// </summary>
    public async Task<IReadOnlyList<InternationalEntryDto>> ListEntriesAsync(
        long pageId,
        CancellationToken cancellationToken = default)
    {
        _ = await RequirePageAsync(pageId, cancellationToken);
        var entries = await repository.ListEntriesAsync(pageId, cancellationToken);
        return BuildEntryTree(entries);
    }

    /// <summary>
    /// 创建页面条目节点
    /// </summary>
    public async Task<InternationalEntryDto> CreateEntryAsync(
        long pageId,
        CreateInternationalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = await RequirePageAsync(pageId, cancellationToken);
        var parentId = await NormalizeParentIdAsync(pageId, request.ParentId, cancellationToken);
        var key = NormalizeKey(request.Key, nameof(request.Key));
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

        var created = await repository.InsertEntryAsync(entry, ToTranslations(request.Translations), cancellationToken);
        created.Translations = ToTranslations(request.Translations).ToList();
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return MapEntryDto(created, []);
    }

    /// <summary>
    /// 更新页面条目节点
    /// </summary>
    public async Task<InternationalEntryDto> UpdateEntryAsync(
        long entryId,
        UpdateInternationalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        var key = NormalizeKey(request.Key, nameof(request.Key));
        await EnsureSiblingKeyUniqueAsync(entry.PageId, entry.ParentId, key, entry.Id, cancellationToken);

        entry.Key = key;
        entry.Remark = request.Remark?.Trim();
        entry.SortOrder = request.SortOrder;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        var translations = ToTranslations(request.Translations);
        await repository.UpdateEntryAsync(entry, translations, cancellationToken);
        entry.Translations = translations.ToList();
        var page = await RequirePageAsync(entry.PageId, cancellationToken);
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return MapEntryDto(entry, []);
    }

    /// <summary>
    /// 删除页面条目节点及其子节点
    /// </summary>
    public async Task DeleteEntryAsync(long entryId, CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        await repository.DeleteEntryAsync(entryId, cancellationToken);
        var page = await RequirePageAsync(entry.PageId, cancellationToken);
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
    }

    /// <summary>
    /// 请求 AI 翻译条目。
    /// </summary>
    public async Task<AiBusinessResponse> TranslateEntryAsync(
        long entryId,
        TranslateInternationalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await RequireEntryAsync(entryId, cancellationToken);
        var source = GetTranslationValue(entry.Translations, DefaultLocale) ?? entry.Translations.FirstOrDefault()?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new InvalidOperationException($"International entry '{entryId}' has no source text.");
        }

        var targetLocales = request.TargetLocales.Count == 0 ? ["en-US", "zh-HK", "zh-TW"] : request.TargetLocales;
        var response = await aiClient.InvokeAsync(new AiBusinessRequest(
            TranslateBusinessKey,
            Model: request.Model,
            PromptOverride: request.PromptOverride,
            PromptVariables: new Dictionary<string, string>
            {
                ["sourceLocale"] = DefaultLocale,
                ["targetLocales"] = string.Join(", ", targetLocales),
                ["content"] = source
            },
            KnowledgeText: request.KnowledgeText,
            KnowledgeTextMode: request.KnowledgeTextMode,
            Context: new Dictionary<string, string>
            {
                ["entryId"] = entryId.ToString(),
                ["targetLocales"] = string.Join(",", targetLocales)
            },
            CallbackName: TranslationCompletedTopic,
            Input: $"Translate the following {DefaultLocale} text to {string.Join(", ", targetLocales)} and return a JSON object whose keys are locales and values are translations.\n\n{source}"),
            cancellationToken);

        if (response.Success)
        {
            await ApplyAiTranslationAsync(response, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// 应用 AI 翻译结果。
    /// </summary>
    public async Task ApplyAiTranslationAsync(AiBusinessResponse response, CancellationToken cancellationToken = default)
    {
        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            return;
        }

        if (response.Context is null ||
            !response.Context.TryGetValue("entryId", out var entryIdText) ||
            !long.TryParse(entryIdText, out var entryId))
        {
            return;
        }

        var translations = ParseTranslationContent(response.Content)
            .Select(item => new InternationalEntryTranslation { Locale = item.Key, Value = item.Value })
            .ToList();
        if (translations.Count == 0)
        {
            return;
        }

        var entry = await RequireEntryAsync(entryId, cancellationToken);
        await repository.UpsertEntryTranslationsAsync(entryId, translations, cancellationToken);
        var page = await RequirePageAsync(entry.PageId, cancellationToken);
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
    }

    /// <summary>
    /// 发布页面版本
    /// </summary>
    public async Task<InternationalPageDto> PublishPageVersionAsync(long pageId, CancellationToken cancellationToken = default)
    {
        await repository.IncreasePageVersionAsync(pageId, cancellationToken);
        var page = await RequirePageAsync(pageId, cancellationToken);
        await InvalidatePageAsync(page.Id, page.PageKey, cancellationToken);
        return mapper.Map<InternationalPageDto>(page);
    }

    /// <summary>
    /// 获取当前国际化版本
    /// </summary>
    public async Task<InternationalVersionDto> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var model = await cache.Get<InternationalVersionCacheModel>()
            .Where(model => model.Id == InternationalCacheKeys.VersionKey)
            .GetOrCreateAsync(async ct =>
            {
                var version = await repository.GetVersionAsync(ct);
                var pages = await repository.ListPageVersionsAsync(ct);
                // 版本接口同时返回总版本和页面版本，前端可先做整体判断，再按页面细粒度刷新。
                var pageVersions = pages
                    .Select(page => new InternationalPageVersionDto(page.PageKey, page.Version))
                    .ToList();
                return new InternationalVersionCacheModel
                {
                    Value = new InternationalVersionDto(version, pageVersions)
                };
            }, cancellationToken);
        return model.Value;
    }

    /// <summary>
    /// 获取前端可直接合并的国际化资源包
    /// </summary>
    public async Task<InternationalBundleDto> GetBundleAsync(CancellationToken cancellationToken = default)
    {
        var model = await cache.Get<InternationalBundleCacheModel>()
            .Where(model => model.Id == InternationalCacheKeys.BundleKey)
            .GetOrCreateAsync(async ct =>
            {
                var pages = await repository.ListPagesWithEntriesAsync(ct);
                var messages = new Dictionary<string, object>(StringComparer.Ordinal);

                foreach (var page in pages)
                {
                    // 每个页面合并到对应语言根节点下，最终结构形如 { "zh-CN": { "pageKey": { ... } } }。
                    MergePageMessages(messages, page);
                }

                var version = await repository.GetVersionAsync(ct);
                return new InternationalBundleCacheModel
                {
                    Value = new InternationalBundleDto(version, messages)
                };
            }, cancellationToken);
        return model.Value;
    }

    /// <summary>
    /// 获取前端可直接合并的单页面国际化资源包
    /// </summary>
    public async Task<InternationalPageBundleDto> GetPageBundleAsync(
        string pageKey,
        CancellationToken cancellationToken = default)
    {
        pageKey = NormalizeKey(pageKey, nameof(pageKey));
        var model = await cache.Get<InternationalPageBundleCacheModel>()
            .Where(model => model.PageKey == pageKey)
            .GetOrCreateAsync(async ct =>
            {
                var page = await repository.GetPageWithEntriesByKeyAsync(pageKey, ct)
                           ?? throw new KeyNotFoundException($"International page '{pageKey}' was not found.");
                var messages = new Dictionary<string, object>(StringComparer.Ordinal);
                MergePageMessages(messages, page);
                return new InternationalPageBundleCacheModel
                {
                    PageKey = page.PageKey,
                    PageId = page.Id,
                    Value = new InternationalPageBundleDto(page.PageKey, page.Version, messages)
                };
            }, cancellationToken);
        return model.Value;
    }

    /// <summary>
    /// 失效国际化模块的全局缓存。
    /// </summary>
    private async Task InvalidateAllAsync(CancellationToken cancellationToken)
    {
        await cacheInvalidator.InvalidateTagAsync(InternationalCacheKeys.AllTag, cancellationToken);
    }

    /// <summary>
    /// 失效指定页面相关的全量缓存、页面 ID 缓存和页面 Key 缓存。
    /// </summary>
    private async Task InvalidatePageAsync(long pageId, string pageKey, CancellationToken cancellationToken)
    {
        // 页面变更会同时影响版本接口、全量资源包和单页面资源包，因此需要同时清理全局与页面级 tag。
        await InvalidateAllAsync(cancellationToken);
        await cacheInvalidator.InvalidateTagAsync(InternationalCacheKeys.PageIdTag(pageId), cancellationToken);
        await cacheInvalidator.InvalidateTagAsync(InternationalCacheKeys.PageTag(pageKey), cancellationToken);
    }

    /// <summary>
    /// 获取页面，不存在时抛出业务异常。
    /// </summary>
    private async Task<InternationalPage> RequirePageAsync(long id, CancellationToken cancellationToken)
    {
        var page = await repository.GetPageAsync(id, cancellationToken);
        return page ?? throw new KeyNotFoundException($"International page '{id}' was not found.");
    }

    /// <summary>
    /// 获取条目，不存在时抛出业务异常。
    /// </summary>
    private async Task<InternationalEntry> RequireEntryAsync(long id, CancellationToken cancellationToken)
    {
        var entry = await repository.GetEntryAsync(id, cancellationToken);
        return entry ?? throw new KeyNotFoundException($"International entry '{id}' was not found.");
    }

    /// <summary>
    /// 校验父级条目并规范化空父级。
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
            throw new InvalidOperationException($"International parent entry '{parentId}' does not belong to page '{pageId}'.");
        }

        return parentId;
    }

    /// <summary>
    /// 确保同一页面、同一父级下的条目 Key 唯一。
    /// </summary>
    private async Task EnsureSiblingKeyUniqueAsync(
        long pageId,
        long? parentId,
        string key,
        long? ignoredId,
        CancellationToken cancellationToken)
    {
        var siblings = await repository.ListEntriesAsync(pageId, cancellationToken);
        // 条目 Key 只要求同级唯一，允许不同父节点下复用相同的业务键名。
        var exists = siblings.Any(entry =>
            entry.ParentId == parentId &&
            entry.Id != ignoredId &&
            string.Equals(entry.Key, key, StringComparison.Ordinal));
        if (exists)
        {
            throw new InvalidOperationException($"International entry key '{key}' already exists in the same level.");
        }
    }

    /// <summary>
    /// 将数据库中的扁平条目列表构造成前端需要的树形 DTO。
    /// </summary>
    private IReadOnlyList<InternationalEntryDto> BuildEntryTree(IReadOnlyList<InternationalEntry> entries)
    {
        // 先按 ParentId 分组，递归构造时就能通过父级 ID 快速取到直接子节点。
        var groups = entries
            .GroupBy(entry => entry.ParentId)
            .ToDictionary(
                group => ParentKey(group.Key),
                group => group.OrderBy(entry => entry.SortOrder).ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList());
        return BuildEntryDtos(groups, 0);
    }

    /// <summary>
    /// 递归构造指定父级下的条目 DTO 列表。
    /// </summary>
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

    /// <summary>
    /// 构造默认语言的嵌套消息对象。
    /// </summary>
    private static Dictionary<string, object> BuildMessageObject(IReadOnlyDictionary<long, List<InternationalEntry>> groups, long parentId)
    {
        var messages = new Dictionary<string, object>(StringComparer.Ordinal);
        if (!groups.TryGetValue(parentId, out var entries))
        {
            return messages;
        }

        foreach (var entry in entries)
        {
            messages[entry.Key] = BuildMessageValue(groups, entry, DefaultLocale);
        }

        return messages;
    }

    /// <summary>
    /// 构造指定语言的嵌套消息对象。
    /// </summary>
    private static object BuildMessageObject(IReadOnlyDictionary<long, List<InternationalEntry>> groups, long parentId, string locale)
    {
        var messages = new Dictionary<string, object>(StringComparer.Ordinal);
        if (!groups.TryGetValue(parentId, out var entries))
        {
            return messages;
        }

        foreach (var entry in entries)
        {
            messages[entry.Key] = BuildMessageValue(groups, entry, locale);
        }

        return messages;
    }

    /// <summary>
    /// 根据条目类型构造叶子文案或子级消息对象。
    /// </summary>
    private static object BuildMessageValue(IReadOnlyDictionary<long, List<InternationalEntry>> groups, InternationalEntry entry, string locale)
    {
        if (groups.ContainsKey(entry.Id))
        {
            // 有子节点时当前节点是对象容器，不直接输出自身翻译文本。
            return BuildMessageObject(groups, entry.Id, locale);
        }

        // 叶子节点优先返回请求语言，缺失时回退默认语言，仍缺失时给前端空字符串。
        return GetTranslationValue(entry.Translations, locale)
               ?? GetTranslationValue(entry.Translations, DefaultLocale)
               ?? string.Empty;
    }

    /// <summary>
    /// 将单个页面的条目合并进全量资源包。
    /// </summary>
    private static void MergePageMessages(Dictionary<string, object> messages, InternationalPage page)
    {
        // 全量资源包按语言分组，语言集合来自页面翻译，并强制包含默认语言。
        var locales = page.Entries
            .SelectMany(entry => entry.Translations)
            .Select(translation => translation.Locale)
            .Append(DefaultLocale)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(locale => locale, StringComparer.Ordinal)
            .ToArray();
        // 条目按父级分组后递归转换，保持排序值和 key 的稳定输出顺序。
        var entryGroups = page.Entries
            .GroupBy(entry => entry.ParentId)
            .ToDictionary(group => ParentKey(group.Key),
                group => group.OrderBy(entry => entry.SortOrder).ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList());

        foreach (var locale in locales)
        {
            var localeRoot = GetOrCreateObject(messages, locale);
            localeRoot[page.PageKey] = BuildMessageObject(entryGroups, 0, locale);
        }
    }

    /// <summary>
    /// 读取指定语言的翻译文本。
    /// </summary>
    private static string? GetTranslationValue(IReadOnlyList<InternationalEntryTranslation> translations, string locale) =>
        translations.FirstOrDefault(t => string.Equals(t.Locale, locale, StringComparison.Ordinal))?.Value;

    /// <summary>
    /// 将空父级转换成字典分组使用的根节点键。
    /// </summary>
    private static long ParentKey(long? parentId) => parentId ?? 0;

    /// <summary>
    /// 从根对象读取指定 key 的子对象，不存在或类型不匹配时重新创建。
    /// </summary>
    private static Dictionary<string, object> GetOrCreateObject(IDictionary<string, object> root, string key)
    {
        if (root.TryGetValue(key, out var existing) && existing is Dictionary<string, object> dictionary)
        {
            return dictionary;
        }

        dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
        root[key] = dictionary;
        return dictionary;
    }

    /// <summary>
    /// 将请求 DTO 转成领域翻译集合，并按语言去重。
    /// </summary>
    private static IReadOnlyList<InternationalEntryTranslation> ToTranslations(IReadOnlyList<InternationalEntryTranslationDto> translations) =>
        translations
            .Select(item => new InternationalEntryTranslation
            {
                Locale = NormalizeRequired(item.Locale, nameof(item.Locale)),
                Value = item.Value
            })
            // 同一个请求内重复提交同一语言时以后出现的值为准，避免数据库中产生重复语言行。
            .GroupBy(item => item.Locale, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(item => item.Locale, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// 规范化页面 Key 或条目 Key，并阻止路径分隔符进入层级键。
    /// </summary>
    private static string NormalizeKey(string value, string name)
    {
        var normalized = NormalizeRequired(value, name);
        if (normalized.Contains('.') || normalized.Contains(':') || normalized.Contains('/'))
        {
            throw new ArgumentException($"{name} cannot contain '.', ':' or '/'.", name);
        }

        return normalized;
    }

    /// <summary>
    /// 校验必填字符串并去除首尾空白。
    /// </summary>
    private static string NormalizeRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value.Trim();
    }

    /// <summary>
    /// 解析 AI 返回的翻译 JSON。
    /// </summary>
    private static IReadOnlyDictionary<string, string> ParseTranslationContent(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            content = content[start..(end + 1)];
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(
                   content,
                   new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? new Dictionary<string, string>();
    }

    private InternationalEntryDto MapEntryDto(InternationalEntry entry, IReadOnlyList<InternationalEntryDto> children) =>
        mapper.Map<InternationalEntryDto>(entry) with { Children = children };
}


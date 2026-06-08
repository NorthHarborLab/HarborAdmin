using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化资源包构建器。
/// </summary>
internal static class InternationalBundleBuilder
{
    /// <summary>
    /// 将单个页面的条目合并进全量资源包。
    /// </summary>
    internal static void MergePageMessages(Dictionary<string, object> messages, InternationalPage page)
    {
        var locales = page.Entries
            .SelectMany(entry => entry.Translations)
            .Select(translation => translation.Locale)
            .Append(InternationalConstants.DefaultLocale)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(locale => locale, StringComparer.Ordinal)
            .ToArray();
        var entryGroups = page.Entries
            .GroupBy(entry => entry.ParentId)
            .ToDictionary(group => ParentKey(group.Key),
                group => group.OrderBy(entry => entry.SortOrder).ThenBy(entry => entry.Key, StringComparer.Ordinal).ToList());

        foreach (var locale in locales)
        {
            // 每个 locale 都有独立根对象，页面资源挂在 pageKey 下供前端按页面合并。
            var localeRoot = GetOrCreateObject(messages, locale);
            localeRoot[page.PageKey] = BuildMessageObject(entryGroups, 0, locale);
        }
    }

    /// <summary>
    /// 构建指定父级下的资源对象。
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
    /// 构建单个条目的资源值；有子节点时返回对象，叶子节点返回文本。
    /// </summary>
    private static object BuildMessageValue(IReadOnlyDictionary<long, List<InternationalEntry>> groups, InternationalEntry entry, string locale)
    {
        if (groups.ContainsKey(entry.Id))
        {
            return BuildMessageObject(groups, entry.Id, locale);
        }

        // 目标语言缺失时回退默认语言，仍缺失则返回空字符串，避免前端出现 undefined。
        return GetTranslationValue(entry.Translations, locale)
               ?? GetTranslationValue(entry.Translations, InternationalConstants.DefaultLocale)
               ?? string.Empty;
    }

    /// <summary>
    /// 获取指定 locale 的翻译文本。
    /// </summary>
    private static string? GetTranslationValue(IReadOnlyList<InternationalEntryTranslation> translations, string locale) =>
        translations.FirstOrDefault(t => string.Equals(t.Locale, locale, StringComparison.Ordinal))?.Value;

    /// <summary>
    /// 将空父级归并到树根 key。
    /// </summary>
    private static long ParentKey(long? parentId) => parentId ?? 0;

    /// <summary>
    /// 获取或创建指定 key 下的对象节点。
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
}

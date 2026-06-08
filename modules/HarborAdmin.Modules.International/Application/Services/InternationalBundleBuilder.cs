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
            var localeRoot = GetOrCreateObject(messages, locale);
            localeRoot[page.PageKey] = BuildMessageObject(entryGroups, 0, locale);
        }
    }

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

    private static object BuildMessageValue(IReadOnlyDictionary<long, List<InternationalEntry>> groups, InternationalEntry entry, string locale)
    {
        if (groups.ContainsKey(entry.Id))
        {
            return BuildMessageObject(groups, entry.Id, locale);
        }

        return GetTranslationValue(entry.Translations, locale)
               ?? GetTranslationValue(entry.Translations, InternationalConstants.DefaultLocale)
               ?? string.Empty;
    }

    private static string? GetTranslationValue(IReadOnlyList<InternationalEntryTranslation> translations, string locale) =>
        translations.FirstOrDefault(t => string.Equals(t.Locale, locale, StringComparison.Ordinal))?.Value;

    private static long ParentKey(long? parentId) => parentId ?? 0;

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

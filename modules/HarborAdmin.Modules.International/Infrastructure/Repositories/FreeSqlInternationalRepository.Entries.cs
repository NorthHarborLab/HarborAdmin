using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的国际化条目仓储实现。
/// </summary>
public sealed partial class FreeSqlInternationalRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InternationalEntry>> ListEntriesAsync(long pageId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalEntry>()
            .Where(e => e.PageId == pageId)
            .IncludeMany(e => e.Translations)
            .OrderBy(e => e.ParentId)
            .OrderBy(e => e.SortOrder)
            .OrderBy(e => e.Key)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalEntry?> GetEntryAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalEntry>()
            .Where(e => e.Id == id)
            .IncludeMany(e => e.Translations)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalEntry> InsertEntryAsync(InternationalEntry entry, IReadOnlyList<InternationalEntryTranslation> translations,
        CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(entry).ExecuteInsertedAsync(cancellationToken);
        entry.Id = inserted.First().Id;

        foreach (var translation in translations)
        {
            // 翻译行依赖条目主键，必须在条目插入后逐条回填 EntryId。
            translation.EntryId = entry.Id;
            await FreeSql.Insert(translation).ExecuteAffrowsAsync(cancellationToken);
        }

        return entry;
    }

    /// <inheritdoc />
    public async Task UpdateEntryAsync(InternationalEntry entry, IReadOnlyList<InternationalEntryTranslation> translations,
        CancellationToken cancellationToken = default)
    {
        await FreeSql.Update<InternationalEntry>().SetSource(entry).ExecuteAffrowsAsync(cancellationToken);
        // 翻译列表按请求整体替换，避免留下被前端删除的旧语言文本。
        await FreeSql.Delete<InternationalEntryTranslation>().Where(t => t.EntryId == entry.Id).ExecuteAffrowsAsync(cancellationToken);

        foreach (var translation in translations)
        {
            // 新翻译需要重新绑定到当前条目。
            translation.EntryId = entry.Id;
            await FreeSql.Insert(translation).ExecuteAffrowsAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UpsertEntryTranslationsAsync(long entryId, IReadOnlyList<InternationalEntryTranslation> translations,
        CancellationToken cancellationToken = default)
    {
        foreach (var translation in translations)
        {
            translation.EntryId = entryId;
            await FreeSql.Delete<InternationalEntryTranslation>()
                .Where(t => t.EntryId == entryId && t.Locale == translation.Locale)
                .ExecuteAffrowsAsync(cancellationToken);
            await FreeSql.Insert(translation).ExecuteAffrowsAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteEntryAsync(long id, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryAsync(id, cancellationToken);
        if (entry is null)
        {
            return;
        }

        var entries = await FreeSql.Select<InternationalEntry>()
            .Where(e => e.PageId == entry.PageId)
            .ToListAsync(cancellationToken);
        var deleteIds = CollectDescendantIds(entries, id);
        if (deleteIds.Count == 0)
        {
            return;
        }

        // 先删除整棵子树的翻译，再删除条目本身，保证不会留下孤立翻译。
        await FreeSql.Delete<InternationalEntryTranslation>().Where(t => deleteIds.Contains(t.EntryId)).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<InternationalEntry>().Where(e => deleteIds.Contains(e.Id)).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <summary>
    /// 收集指定条目及其所有后代条目的主键。
    /// </summary>
    private static List<long> CollectDescendantIds(IReadOnlyList<InternationalEntry> entries, long rootId)
    {
        // 先建立父子索引，再用栈遍历，避免递归层级过深时产生调用栈风险。
        var children = entries
            .Where(e => e.ParentId is not null)
            .GroupBy(e => e.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList());
        var ids = new List<long>();
        var stack = new Stack<long>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            ids.Add(id);
            if (!children.TryGetValue(id, out var childIds))
            {
                continue;
            }

            foreach (var childId in childIds)
            {
                // 将子节点压栈后继续向下收集整棵子树。
                stack.Push(childId);
            }
        }

        return ids;
    }
}

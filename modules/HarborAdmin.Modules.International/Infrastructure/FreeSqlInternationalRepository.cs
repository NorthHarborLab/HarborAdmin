using HarborAdmin.Modules.International.Contracts;
using HarborAdmin.Modules.International.Domain;

namespace HarborAdmin.Modules.International.Infrastructure;

/// <summary>
/// 基于 FreeSql 的国际化仓储实现
/// </summary>
public sealed class FreeSqlInternationalRepository(IInternationalDbContext db) : IInternationalRepository
{
    /// <summary>
    /// 国际化模块使用的 FreeSql 实例。
    /// </summary>
    private IFreeSql FreeSql => db.Orm;

    /// <inheritdoc />
    public Task<IReadOnlyList<InternationalPage>> ListPagesAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<InternationalPage>()
            .OrderBy(p => p.PageKey)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<InternationalPage>)t.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<InternationalPage>> ListPagesWithEntriesAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<InternationalPage>()
            // 全量资源包需要一次加载页面、条目和翻译，避免服务层逐页触发多次查询。
            .IncludeMany(p => p.Entries, then => then
                .IncludeMany(e => e.Translations)
                .OrderBy(e => e.ParentId)
                .OrderBy(e => e.SortOrder)
                .OrderBy(e => e.Key))
            .OrderBy(p => p.PageKey)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<InternationalPage>)t.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<InternationalPage>> ListPageVersionsAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<InternationalPage>()
            .OrderBy(p => p.PageKey)
            .ToListAsync(p => new InternationalPage
            {
                PageKey = p.PageKey,
                Version = p.Version
            }, cancellationToken)
            .ContinueWith(t => (IReadOnlyList<InternationalPage>)t.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage?> GetPageAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>().Where(p => p.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage?> GetPageByKeyAsync(
        string pageKey,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            .Where(p => p.PageKey == pageKey)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage?> GetPageWithEntriesByKeyAsync(
        string pageKey,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            .Where(p => p.PageKey == pageKey)
            // 单页面资源包同样需要完整条目树与翻译集合。
            .IncludeMany(p => p.Entries, then => then
                .IncludeMany(e => e.Translations)
                .OrderBy(e => e.ParentId)
                .OrderBy(e => e.SortOrder)
                .OrderBy(e => e.Key))
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage> InsertPageAsync(InternationalPage page, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(page).ExecuteInsertedAsync(cancellationToken);
        // FreeSql 返回数据库生成的主键，回填到传入实体便于调用方继续使用。
        page.Id = inserted.First().Id;
        return page;
    }

    /// <inheritdoc />
    public Task UpdatePageAsync(InternationalPage page, CancellationToken cancellationToken = default) =>
        FreeSql.Update<InternationalPage>().SetSource(page).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeletePageAsync(long id, CancellationToken cancellationToken = default)
    {
        var entryIds = await FreeSql.Select<InternationalEntry>()
            .Where(e => e.PageId == id)
            .ToListAsync(e => e.Id, cancellationToken);
        if (entryIds.Count > 0)
        {
            // 页面删除需要先清理翻译，再清理条目，最后删除页面自身。
            await FreeSql.Delete<InternationalEntryTranslation>().Where(t => entryIds.Contains(t.EntryId)).ExecuteAffrowsAsync(cancellationToken);
            await FreeSql.Delete<InternationalEntry>().Where(e => entryIds.Contains(e.Id)).ExecuteAffrowsAsync(cancellationToken);
        }

        await FreeSql.Delete<InternationalPage>().Where(p => p.Id == id).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<InternationalEntry>> ListEntriesAsync(
        long pageId,
        CancellationToken cancellationToken = default)
        =>
            FreeSql.Select<InternationalEntry>()
                .Where(e => e.PageId == pageId)
                .IncludeMany(e => e.Translations)
                .OrderBy(e => e.ParentId)
                .OrderBy(e => e.SortOrder)
                .OrderBy(e => e.Key)
                .ToListAsync(cancellationToken)
                .ContinueWith(t => (IReadOnlyList<InternationalEntry>)t.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalEntry?> GetEntryAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalEntry>()
            .Where(e => e.Id == id)
            .IncludeMany(e => e.Translations)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalEntry> InsertEntryAsync(
        InternationalEntry entry,
        IReadOnlyList<InternationalEntryTranslation> translations,
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
    public async Task UpdateEntryAsync(
        InternationalEntry entry,
        IReadOnlyList<InternationalEntryTranslation> translations,
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
    public async Task DeleteEntryAsync(long id, CancellationToken cancellationToken = default)
    {
        var entries = await FreeSql.Select<InternationalEntry>().ToListAsync(cancellationToken);
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

    /// <inheritdoc />
    public async Task<int> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var pages = await FreeSql.Select<InternationalPage>()
            .OrderBy(p => p.PageKey)
            .ToListAsync(p => new { p.PageKey, p.Version }, cancellationToken);
        if (pages.Count == 0)
        {
            return 0;
        }

        var version = pages.Count;
        foreach (var page in pages)
        {
            unchecked
            {
                // 使用稳定顺序下的页面版本和 PageKey 混合出总版本，允许 int 溢出但保持确定性。
                version = (version * 397) ^ page.Version;
                foreach (var ch in page.PageKey)
                {
                    version = (version * 397) ^ ch;
                }
            }
        }

        return version;
    }

    /// <inheritdoc />
    public async Task IncreasePageVersionAsync(long pageId, CancellationToken cancellationToken = default)
    {
        var page = await GetPageAsync(pageId, cancellationToken);
        if (page is null)
        {
            return;
        }

        page.Version += 1;
        page.UpdatedAt = DateTimeOffset.UtcNow;
        await UpdatePageAsync(page, cancellationToken);
    }
}

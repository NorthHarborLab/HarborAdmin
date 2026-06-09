using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的国际化页面仓储实现。
/// </summary>
public sealed partial class FreeSqlInternationalRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InternationalPage>> ListPagesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            .OrderBy(p => p.FullPath)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InternationalPage>> ListPagesWithEntriesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            // 全量资源包需要一次加载页面、条目和翻译，避免服务层逐页触发多次查询。
            .IncludeMany(p => p.Entries, then => then
                .IncludeMany(e => e.Translations)
                .OrderBy(e => e.ParentId)
                .OrderBy(e => e.SortOrder)
                .OrderBy(e => e.Key))
            .OrderBy(p => p.FullPath)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage?> GetPageAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>().Where(p => p.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage?> GetPageByPathAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            .Where(p => p.FullPath == path)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<InternationalPage?> GetPageWithEntriesByPathAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            .Where(p => p.FullPath == path)
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
    public Task UpdatePagesAsync(IReadOnlyList<InternationalPage> pages, CancellationToken cancellationToken = default) =>
        pages.Count == 0
            ? Task.CompletedTask
            : FreeSql.Update<InternationalPage>().SetSource(pages).ExecuteAffrowsAsync(cancellationToken);

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
}

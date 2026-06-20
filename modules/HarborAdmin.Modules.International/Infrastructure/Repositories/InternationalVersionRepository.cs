using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Domain.Entities;
using HarborAdmin.Modules.International.Infrastructure.Contexts;

namespace HarborAdmin.Modules.International.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的国际化版本仓储实现。
/// </summary>
public sealed class InternationalVersionRepository(IInternationalDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IInternationalDbContext>(db, unitOfWorkManager), IInternationalVersionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InternationalPage>> ListPageVersionsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<InternationalPage>()
            .OrderBy(p => p.FullPath)
            .ToListAsync(p => new InternationalPage
            {
                PageKey = p.PageKey,
                FullPath = p.FullPath,
                Version = p.Version
            }, cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var pages = await FreeSql.Select<InternationalPage>()
            .OrderBy(p => p.FullPath)
            .ToListAsync(p => new { p.FullPath, p.Version }, cancellationToken);
        if (pages.Count == 0)
        {
            return 0;
        }

        var version = pages.Count;
        foreach (var page in pages)
        {
            unchecked
            {
                // 使用稳定顺序下的页面版本和 FullPath 混合出总版本，允许 int 溢出但保持确定性。
                version = (version * 397) ^ page.Version;
                foreach (var ch in page.FullPath)
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
        var page = await FreeSql.Select<InternationalPage>()
            .Where(p => p.Id == pageId)
            .FirstAsync(cancellationToken);
        if (page is null)
        {
            return;
        }

        page.Version += 1;
        await FreeSql.Update<InternationalPage>().SetSource(page).ExecuteAffrowsAsync(cancellationToken);
    }
}

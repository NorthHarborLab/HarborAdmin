using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的配置中心发布仓储实现。
/// </summary>
public sealed class ConfigReleaseRepository(IConfigCenterDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlModuleRepository<IConfigCenterDbContext>(db), IConfigReleaseRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigRelease>> ListReleasesAsync(string appId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId)
            .OrderByDescending(r => r.Version)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigRelease?> GetLatestReleaseAsync(string appId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId)
            .OrderByDescending(r => r.Version)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigRelease?> GetReleaseByIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigRelease>()
            .Where(r => r.Id == releaseId)
            .IncludeMany(r => r.Items)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigReleaseItem>> ListReleaseItemsAsync(long releaseId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigReleaseItem>()
            .Where(i => i.ReleaseId == releaseId)
            .OrderBy(i => i.Group)
            .OrderBy(i => i.Key)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigRelease> InsertReleaseAsync(
        ConfigRelease release,
        IReadOnlyList<ConfigReleaseItem> items,
        CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbContext.DbKey);
        using (DbContext.Bind(DbContext.DbKey, uow.Orm))
        {
            var inserted = await FreeSql.Insert(release).ExecuteInsertedAsync(cancellationToken);
            release.Id = inserted.First().Id;
            if (release.Id == 0)
            {
                release.Id = await FreeSql.Select<ConfigRelease>()
                    .Where(r => r.AppId == release.AppId && r.Version == release.Version)
                    .FirstAsync(r => r.Id, cancellationToken);
            }

            foreach (var item in items)
            {
                item.ReleaseId = release.Id;
                await FreeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken);
            }
        }

        uow.Commit();
        return release;
    }
}

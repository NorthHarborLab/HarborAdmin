using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的配置中心发布仓储实现。
/// </summary>
public sealed partial class FreeSqlConfigCenterRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigRelease>> ListReleasesAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId && r.Environment == environment)
            .OrderByDescending(r => r.Version)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigRelease>)t.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigRelease?> GetLatestReleaseAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId && r.Environment == environment)
            .OrderByDescending(r => r.Version)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigRelease?> GetReleaseByIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigRelease>().Where(r => r.Id == releaseId).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigReleaseItem>> ListReleaseItemsAsync(long releaseId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigReleaseItem>()
            .Where(i => i.ReleaseId == releaseId)
            .OrderBy(i => i.Group)
            .OrderBy(i => i.Key)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigReleaseItem>)t.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigRelease> InsertReleaseAsync(
        ConfigRelease release,
        IReadOnlyList<ConfigReleaseItem> items,
        CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(release).ExecuteInsertedAsync(cancellationToken);
        release.Id = inserted.First().Id;
        if (release.Id == 0)
        {
            release.Id = await FreeSql.Select<ConfigRelease>()
                .Where(r => r.AppId == release.AppId && r.Environment == release.Environment && r.Version == release.Version)
                .FirstAsync(r => r.Id, cancellationToken);
        }

        foreach (var item in items)
        {
            item.ReleaseId = release.Id;
            await FreeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken);
        }

        return release;
    }
}

using HarborAdmin.Modules.ConfigCenter.Contracts;
using HarborAdmin.Modules.ConfigCenter.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure;

/// <summary>
/// 基于 FreeSql 的 <see cref="IConfigCenterRepository"/> 实现。
/// </summary>
/// <param name="freeSql">FreeSql 实例。</param>
public sealed class FreeSqlConfigCenterRepository(IFreeSql freeSql) : IConfigCenterRepository
{
    /// <inheritdoc cref="IConfigCenterRepository.ListApplicationsAsync" />
    public Task<IReadOnlyList<ConfigApplication>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigApplication>().OrderBy(a => a.AppId).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigApplication>)t.Result, cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.GetApplicationByAppIdAsync" />
    public Task<ConfigApplication?> GetApplicationByAppIdAsync(string appId, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigApplication>().Where(a => a.AppId == appId).FirstAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.InsertApplicationAsync" />
    public async Task<ConfigApplication> InsertApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default)
    {
        var inserted = await freeSql.Insert(application).ExecuteInsertedAsync(cancellationToken);
        application.Id = inserted.First().Id;
        return application;
    }

    /// <inheritdoc cref="IConfigCenterRepository.UpdateApplicationAsync" />
    public Task UpdateApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default) =>
        freeSql.Update<ConfigApplication>().SetSource(application).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.DeleteApplicationAsync" />
    public async Task DeleteApplicationAsync(string appId, CancellationToken cancellationToken = default)
    {
        var releaseIds = await freeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId)
            .ToListAsync(r => r.Id, cancellationToken);

        if (releaseIds.Count > 0)
        {
            await freeSql.Delete<ConfigReleaseItem>()
                .Where(i => releaseIds.Contains(i.ReleaseId))
                .ExecuteAffrowsAsync(cancellationToken);
        }

        await freeSql.Delete<ConfigItem>().Where(i => i.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
        await freeSql.Delete<ConfigRelease>().Where(r => r.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
        await freeSql.Delete<ConfigApplication>().Where(a => a.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc cref="IConfigCenterRepository.ListItemsAsync" />
    public Task<IReadOnlyList<ConfigItem>> ListItemsAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigItem>()
            .Where(i => i.AppId == appId && i.Environment == environment)
            .OrderBy(i => i.Group)
            .OrderBy(i => i.Key)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigItem>)t.Result, cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.GetItemAsync" />
    public Task<ConfigItem?> GetItemAsync(long id, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigItem>().Where(i => i.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.InsertItemAsync" />
    public async Task<ConfigItem> InsertItemAsync(ConfigItem item, CancellationToken cancellationToken = default)
    {
        var inserted = await freeSql.Insert(item).ExecuteInsertedAsync(cancellationToken);
        item.Id = inserted.First().Id;
        return item;
    }

    /// <inheritdoc cref="IConfigCenterRepository.UpdateItemAsync" />
    public Task UpdateItemAsync(ConfigItem item, CancellationToken cancellationToken = default) =>
        freeSql.Update<ConfigItem>().SetSource(item).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.DeleteItemAsync" />
    public Task DeleteItemAsync(long id, CancellationToken cancellationToken = default) =>
        freeSql.Delete<ConfigItem>().Where(i => i.Id == id).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.ListReleasesAsync" />
    public Task<IReadOnlyList<ConfigRelease>> ListReleasesAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId && r.Environment == environment)
            .OrderByDescending(r => r.Version)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigRelease>)t.Result, cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.GetLatestReleaseAsync" />
    public Task<ConfigRelease?> GetLatestReleaseAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId && r.Environment == environment)
            .OrderByDescending(r => r.Version)
            .FirstAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.GetReleaseByIdAsync" />
    public Task<ConfigRelease?> GetReleaseByIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigRelease>().Where(r => r.Id == releaseId).FirstAsync(cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.ListReleaseItemsAsync" />
    public Task<IReadOnlyList<ConfigReleaseItem>> ListReleaseItemsAsync(long releaseId, CancellationToken cancellationToken = default) =>
        freeSql.Select<ConfigReleaseItem>()
            .Where(i => i.ReleaseId == releaseId)
            .OrderBy(i => i.Group)
            .OrderBy(i => i.Key)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigReleaseItem>)t.Result, cancellationToken);

    /// <inheritdoc cref="IConfigCenterRepository.InsertReleaseAsync" />
    public async Task<ConfigRelease> InsertReleaseAsync(
        ConfigRelease release,
        IReadOnlyList<ConfigReleaseItem> items,
        CancellationToken cancellationToken = default)
    {
        freeSql.Transaction(() =>
        {
            var inserted = freeSql.Insert(release).ExecuteInserted().First();
            release.Id = inserted.Id;
            if (release.Id == 0)
            {
                release.Id = freeSql.Select<ConfigRelease>()
                    .Where(r => r.AppId == release.AppId && r.Environment == release.Environment && r.Version == release.Version)
                    .First(r => r.Id);
            }

            foreach (var item in items)
            {
                item.ReleaseId = release.Id;
                freeSql.Insert(item).ExecuteAffrows();
            }
        });

        return release;
    }
}

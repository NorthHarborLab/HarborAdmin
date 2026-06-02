using HarborAdmin.Modules.ConfigCenter.Contracts;
using HarborAdmin.Modules.ConfigCenter.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure;

/// <summary>
/// 基于 FreeSql 的 <see cref="IConfigCenterRepository"/> 实现。
/// </summary>
public sealed class FreeSqlConfigCenterRepository(IConfigCenterDbContext db) : IConfigCenterRepository
{
    private IFreeSql FreeSql => db.Orm;

    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigApplication>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigApplication>().OrderBy(a => a.AppId).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigApplication>)t.Result, cancellationToken);

    /// <inheritdoc />
    public Task<ConfigApplication?> GetApplicationByAppIdAsync(string appId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigApplication>().Where(a => a.AppId == appId).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigApplication> InsertApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(application).ExecuteInsertedAsync(cancellationToken);
        application.Id = inserted.First().Id;
        return application;
    }

    /// <inheritdoc />
    public Task UpdateApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default) =>
        FreeSql.Update<ConfigApplication>().SetSource(application).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteApplicationAsync(string appId, CancellationToken cancellationToken = default)
    {
        var releaseIds = await FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId)
            .ToListAsync(r => r.Id, cancellationToken);

        if (releaseIds.Count > 0)
        {
            await FreeSql.Delete<ConfigReleaseItem>()
                .Where(i => releaseIds.Contains(i.ReleaseId))
                .ExecuteAffrowsAsync(cancellationToken);
        }

        await FreeSql.Delete<ConfigItem>().Where(i => i.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<ConfigRelease>().Where(r => r.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<ConfigApplication>().Where(a => a.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigItem>> ListItemsAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigItem>()
            .Where(i => i.AppId == appId && i.Environment == environment)
            .OrderBy(i => i.Group)
            .OrderBy(i => i.Key)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigItem>)t.Result, cancellationToken);

    /// <inheritdoc />
    public Task<ConfigItem?> GetItemAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigItem>().Where(i => i.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigItem> InsertItemAsync(ConfigItem item, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(item).ExecuteInsertedAsync(cancellationToken);
        item.Id = inserted.First().Id;
        return item;
    }

    /// <inheritdoc />
    public Task UpdateItemAsync(ConfigItem item, CancellationToken cancellationToken = default) =>
        FreeSql.Update<ConfigItem>().SetSource(item).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task DeleteItemAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<ConfigItem>().Where(i => i.Id == id).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigRelease>> ListReleasesAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId && r.Environment == environment)
            .OrderByDescending(r => r.Version)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigRelease>)t.Result, cancellationToken);

    /// <inheritdoc />
    public Task<ConfigRelease?> GetLatestReleaseAsync(string appId, string environment, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigRelease>()
            .Where(r => r.AppId == appId && r.Environment == environment)
            .OrderByDescending(r => r.Version)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public Task<ConfigRelease?> GetReleaseByIdAsync(long releaseId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigRelease>().Where(r => r.Id == releaseId).FirstAsync(cancellationToken);

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

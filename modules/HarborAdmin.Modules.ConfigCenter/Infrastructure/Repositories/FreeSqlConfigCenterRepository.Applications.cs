namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的配置中心应用仓储实现。
/// </summary>
public sealed partial class FreeSqlConfigCenterRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigApplication>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigApplication>().OrderBy(a => a.AppId).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigApplication?> GetApplicationByAppIdAsync(string appId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigApplication>().Where(a => a.AppId == appId).FirstAsync(cancellationToken);

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
        var application = await FreeSql.Select<ConfigApplication>()
            .Where(a => a.AppId == appId)
            .IncludeMany(a => a.Releases, then => then.IncludeMany(r => r.Items))
            .IncludeMany(a => a.Items)
            .FirstAsync(cancellationToken);
        if (application is null)
        {
            return;
        }

        var releaseItemIds = application.Releases.SelectMany(release => release.Items).Select(item => item.Id).ToArray();
        if (releaseItemIds.Length > 0)
        {
            // 发布项依赖发布记录，先删明细再删发布头，避免留下孤立发布项。
            await FreeSql.Delete<ConfigReleaseItem>().Where(item => releaseItemIds.Contains(item.Id)).ExecuteAffrowsAsync(cancellationToken);
        }

        var releaseIds = application.Releases.Select(release => release.Id).ToArray();
        if (releaseIds.Length > 0)
        {
            await FreeSql.Delete<ConfigRelease>().Where(release => releaseIds.Contains(release.Id)).ExecuteAffrowsAsync(cancellationToken);
        }

        var itemIds = application.Items.Select(item => item.Id).ToArray();
        if (itemIds.Length > 0)
        {
            // 草稿项独立于发布项，也要随应用一起清理。
            await FreeSql.Delete<ConfigItem>().Where(item => itemIds.Contains(item.Id)).ExecuteAffrowsAsync(cancellationToken);
        }

        await FreeSql.Delete<ConfigApplication>().Where(a => a.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
    }
}

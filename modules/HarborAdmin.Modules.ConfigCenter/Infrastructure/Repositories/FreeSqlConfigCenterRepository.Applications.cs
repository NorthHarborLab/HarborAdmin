using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的配置中心应用仓储实现。
/// </summary>
public sealed partial class FreeSqlConfigCenterRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigApplication>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigApplication>().OrderBy(a => a.AppId).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigApplication>)t.Result, cancellationToken);

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
}

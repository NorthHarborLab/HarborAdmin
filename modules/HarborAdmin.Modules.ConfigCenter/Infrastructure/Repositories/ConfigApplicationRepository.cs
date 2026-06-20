using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 配置中心应用实体仓储。
/// </summary>
public sealed class ConfigApplicationRepository(
    IConfigCenterDbContext db,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<ConfigApplication, IConfigCenterDbContext>(db, entityRegistry, unitOfWorkManager), IConfigApplicationRepository
{
    /// <inheritdoc />
    public async Task<ConfigApplication?> GetByAppIdAsync(string appId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigApplication>().Where(application => application.AppId == appId).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteByAppIdAsync(string appId, CancellationToken cancellationToken = default)
    {
        var application = await FreeSql.Select<ConfigApplication>()
            .Where(item => item.AppId == appId)
            .IncludeMany(item => item.Releases, then => then.IncludeMany(release => release.Items))
            .IncludeMany(item => item.Items)
            .FirstAsync(cancellationToken);
        if (application is null)
        {
            return;
        }

        var releaseItemIds = application.Releases.SelectMany(release => release.Items).Select(item => item.Id).ToArray();
        if (releaseItemIds.Length > 0)
        {
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
            await FreeSql.Delete<ConfigItem>().Where(item => itemIds.Contains(item.Id)).ExecuteAffrowsAsync(cancellationToken);
        }

        await FreeSql.Delete<ConfigApplication>().Where(item => item.AppId == appId).ExecuteAffrowsAsync(cancellationToken);
    }
}

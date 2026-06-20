using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 配置中心草稿配置项实体仓储。
/// </summary>
public sealed class ConfigItemRepository(
    IConfigCenterDbContext db,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<ConfigItem, IConfigCenterDbContext>(db, entityRegistry, unitOfWorkManager), IConfigItemRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigItem>> ListByAppIdAsync(string appId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigItem>()
            .Where(item => item.AppId == appId)
            .OrderBy(item => item.Group)
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);
}

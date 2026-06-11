using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 配置中心草稿配置项实体仓储。
/// </summary>
public sealed class ConfigItemRepository(IConfigCenterDbContext db, DbEntityRegistry entityRegistry)
    : FreeSqlEntityRepository<ConfigItem, IConfigCenterDbContext>(db, entityRegistry), IConfigItemRepository
{
    /// <inheritdoc />
    protected override ISelect<ConfigItem> BuildListQuery(ISelect<ConfigItem> query) =>
        query.OrderBy(item => item.Group).OrderBy(item => item.Key);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigItem>> ListByAppIdAsync(string appId, CancellationToken cancellationToken = default) =>
        await BuildListQuery(FreeSql.Select<ConfigItem>().Where(item => item.AppId == appId)).ToListAsync(cancellationToken);
}

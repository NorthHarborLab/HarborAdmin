using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的配置中心草稿配置项仓储实现。
/// </summary>
public sealed partial class FreeSqlConfigCenterRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ConfigItem>> ListItemsAsync(string appId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<ConfigItem>()
            .Where(i => i.AppId == appId)
            .OrderBy(i => i.Group)
            .OrderBy(i => i.Key)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ConfigItem>)t.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigItem?> GetItemAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<ConfigItem>().Where(i => i.Id == id).FirstAsync(cancellationToken);

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
}

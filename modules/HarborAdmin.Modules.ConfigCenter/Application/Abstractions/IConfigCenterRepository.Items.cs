using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

/// <summary>
/// 配置中心草稿配置项仓储接口。
/// </summary>
public partial interface IConfigCenterRepository
{
    /// <summary>
    /// 列出指定应用与环境下的草稿配置项。
    /// </summary>
    Task<IReadOnlyList<ConfigItem>> ListItemsAsync(string appId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键查询草稿配置项。
    /// </summary>
    Task<ConfigItem?> GetItemAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入草稿配置项。
    /// </summary>
    Task<ConfigItem> InsertItemAsync(ConfigItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新草稿配置项。
    /// </summary>
    Task UpdateItemAsync(ConfigItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除草稿配置项。
    /// </summary>
    Task DeleteItemAsync(long id, CancellationToken cancellationToken = default);
}

using HarborAdmin.BuildingBlocks.Abstractions.Repositories;

namespace HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

/// <summary>
/// 配置中心草稿配置项实体仓储。
/// </summary>
public interface IConfigItemRepository : IHarborCrudRepository<ConfigItem>
{
    /// <summary>
    /// 按 AppId 列出草稿配置项。
    /// </summary>
    Task<IReadOnlyList<ConfigItem>> ListByAppIdAsync(string appId, CancellationToken cancellationToken = default);
}

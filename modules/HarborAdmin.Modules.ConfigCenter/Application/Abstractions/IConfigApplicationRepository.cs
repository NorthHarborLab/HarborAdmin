using HarborAdmin.BuildingBlocks.Abstractions.Repositories;

namespace HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

/// <summary>
/// 配置中心应用实体仓储。
/// </summary>
public interface IConfigApplicationRepository : IHarborCrudRepository<ConfigApplication>
{
    /// <summary>
    /// 按 AppId 获取应用。
    /// </summary>
    Task<ConfigApplication?> GetByAppIdAsync(string appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 AppId 删除应用及其配置数据。
    /// </summary>
    Task DeleteByAppIdAsync(string appId, CancellationToken cancellationToken = default);
}

namespace HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

/// <summary>
/// 配置中心应用仓储接口。
/// </summary>
public partial interface IConfigCenterRepository
{
    /// <summary>
    /// 列出所有已注册的应用。
    /// </summary>
    Task<IReadOnlyList<ConfigApplication>> ListApplicationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按应用标识查询应用。
    /// </summary>
    Task<ConfigApplication?> GetApplicationByAppIdAsync(string appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入新应用。
    /// </summary>
    Task<ConfigApplication> InsertApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用元数据。
    /// </summary>
    Task UpdateApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除应用及其下所有草稿、发布记录与快照项。
    /// </summary>
    Task DeleteApplicationAsync(string appId, CancellationToken cancellationToken = default);
}

using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

/// <summary>
/// 配置中心发布仓储接口。
/// </summary>
public partial interface IConfigCenterRepository
{
    /// <summary>
    /// 列出指定应用与环境下的发布历史。
    /// </summary>
    Task<IReadOnlyList<ConfigRelease>> ListReleasesAsync(string appId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最新一次发布记录。
    /// </summary>
    Task<ConfigRelease?> GetLatestReleaseAsync(string appId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按发布主键查询发布记录。
    /// </summary>
    Task<ConfigRelease?> GetReleaseByIdAsync(long releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出某次发布下的全部快照配置项。
    /// </summary>
    Task<IReadOnlyList<ConfigReleaseItem>> ListReleaseItemsAsync(long releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务内插入发布记录及其快照项。
    /// </summary>
    Task<ConfigRelease> InsertReleaseAsync(ConfigRelease release, IReadOnlyList<ConfigReleaseItem> items, CancellationToken cancellationToken = default);
}

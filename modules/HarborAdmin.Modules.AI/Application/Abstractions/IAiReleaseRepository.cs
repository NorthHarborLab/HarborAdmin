using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 配置发布仓储。
/// </summary>
public interface IAiReleaseRepository
{
    /// <summary>
    /// 插入发布并激活。
    /// </summary>
    Task<AiConfigRelease> InsertAndActivateReleaseAsync(AiConfigRelease release, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出发布。
    /// </summary>
    Task<IReadOnlyList<AiConfigRelease>> ListReleasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最新发布。
    /// </summary>
    Task<AiConfigRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取发布。
    /// </summary>
    Task<AiConfigRelease?> GetReleaseAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按版本获取发布。
    /// </summary>
    Task<AiConfigRelease?> GetReleaseByVersionAsync(int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 激活指定发布。
    /// </summary>
    Task ActivateReleaseAsync(long releaseId, CancellationToken cancellationToken = default);
}

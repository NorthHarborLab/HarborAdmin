using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 限额与用量桶仓储。
/// </summary>
public interface IAiQuotaRepository
{
    /// <summary>
    /// 在 Serializable 隔离级别事务中执行配额写操作。
    /// </summary>
    Task ExecuteSerializableAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取供应商限额。
    /// </summary>
    Task<AiProviderQuota?> GetProviderQuotaAsync(long providerId, string? producerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出供应商限额。
    /// </summary>
    Task<IReadOnlyList<AiProviderQuota>> ListProviderQuotasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存供应商限额。
    /// </summary>
    Task<AiProviderQuota> SaveProviderQuotaAsync(AiProviderQuota quota, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配额桶。
    /// </summary>
    Task<AiQuotaBucket?> GetQuotaBucketAsync(
        string providerKey,
        string? model,
        string businessKey,
        string producerKey,
        string windowType,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存配额桶。
    /// </summary>
    Task<AiQuotaBucket> SaveQuotaBucketAsync(AiQuotaBucket bucket, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出配额桶。
    /// </summary>
    Task<IReadOnlyList<AiQuotaBucket>> ListQuotaBucketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按条件列出用量统计用的配额桶。
    /// </summary>
    Task<IReadOnlyList<AiQuotaBucket>> ListUsageQuotaBucketsAsync(
        DateTimeOffset dateFrom,
        DateTimeOffset dateToExclusive,
        string? businessKey,
        string? producerKey,
        string? providerKey,
        string? model,
        CancellationToken cancellationToken = default);
}

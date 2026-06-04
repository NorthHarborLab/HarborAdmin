using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

public partial interface IAiRepository
{
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
    /// 列出模型限额。
    /// </summary>
    Task<IReadOnlyList<AiModelQuota>> ListModelQuotasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存模型限额。
    /// </summary>
    Task<AiModelQuota> SaveModelQuotaAsync(AiModelQuota quota, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除模型限额。
    /// </summary>
    Task DeleteModelQuotaAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配额桶。
    /// </summary>
    Task<AiQuotaBucket?> GetQuotaBucketAsync(string providerKey, string? model, string businessKey, string producerKey, string windowType,
        DateTimeOffset windowStart, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存配额桶。
    /// </summary>
    Task<AiQuotaBucket> SaveQuotaBucketAsync(AiQuotaBucket bucket, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出配额桶。
    /// </summary>
    Task<IReadOnlyList<AiQuotaBucket>> ListQuotaBucketsAsync(CancellationToken cancellationToken = default);
}

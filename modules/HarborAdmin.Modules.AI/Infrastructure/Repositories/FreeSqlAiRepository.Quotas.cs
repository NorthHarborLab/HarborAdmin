using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI 限额仓储实现 partial。
/// </summary>
public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<AiProviderQuota?> GetProviderQuotaAsync(long providerId, string? producerKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProviderQuota>()
            .Where(q => q.ProviderId == providerId && q.ProducerKey == producerKey)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiProviderQuota>> ListProviderQuotasAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProviderQuota>().OrderBy(q => q.ProviderId).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiProviderQuota> SaveProviderQuotaAsync(AiProviderQuota quota, CancellationToken cancellationToken = default)
    {
        if (quota.Id == 0)
        {
            var inserted = await FreeSql.Insert(quota).ExecuteInsertedAsync(cancellationToken);
            quota.Id = inserted.First().Id;
            return quota;
        }

        await FreeSql.Update<AiProviderQuota>().SetSource(quota).ExecuteAffrowsAsync(cancellationToken);
        return quota;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiModelQuota>> ListModelQuotasAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiModelQuota>().OrderBy(q => q.ProviderKey).OrderBy(q => q.ModelName).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiModelQuota> SaveModelQuotaAsync(AiModelQuota quota, CancellationToken cancellationToken = default)
    {
        if (quota.Id == 0)
        {
            var inserted = await FreeSql.Insert(quota).ExecuteInsertedAsync(cancellationToken);
            quota.Id = inserted.First().Id;
            return quota;
        }

        await FreeSql.Update<AiModelQuota>().SetSource(quota).ExecuteAffrowsAsync(cancellationToken);
        return quota;
    }

    /// <inheritdoc />
    public Task DeleteModelQuotaAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AiModelQuota>().Where(q => q.Id == id).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiQuotaBucket?> GetQuotaBucketAsync(
        string providerKey,
        string? model,
        string businessKey,
        string producerKey,
        string windowType,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiQuotaBucket>()
            .Where(b => b.ProviderKey == providerKey && b.Model == model && b.BusinessKey == businessKey &&
                        b.ProducerKey == producerKey && b.WindowType == windowType && b.WindowStart == windowStart)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiQuotaBucket> SaveQuotaBucketAsync(AiQuotaBucket bucket, CancellationToken cancellationToken = default)
    {
        if (bucket.Id == 0)
        {
            var inserted = await FreeSql.Insert(bucket).ExecuteInsertedAsync(cancellationToken);
            bucket.Id = inserted.First().Id;
            return bucket;
        }

        await FreeSql.Update<AiQuotaBucket>().SetSource(bucket).ExecuteAffrowsAsync(cancellationToken);
        return bucket;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiQuotaBucket>> ListQuotaBucketsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiQuotaBucket>().OrderByDescending(b => b.WindowStart).Limit(500).ToListAsync(cancellationToken);
}

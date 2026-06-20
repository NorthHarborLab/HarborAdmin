using System.Data;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 限额与用量桶 FreeSql 仓储。
/// </summary>
public sealed class AiQuotaRepository(IAiDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAiDbContext>(db, unitOfWorkManager), IAiQuotaRepository
{
    /// <inheritdoc />
    public async Task ExecuteSerializableAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(action, cancellationToken, isolationLevel: IsolationLevel.Serializable);
    }

    /// <inheritdoc />
    public async Task<AiProviderQuota?> GetProviderQuotaAsync(long providerId, string? producerKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProviderQuota>()
            .Where(quota => quota.ProviderId == providerId && quota.ProducerKey == producerKey)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiProviderQuota>> ListProviderQuotasAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProviderQuota>().OrderBy(quota => quota.ProviderId).ToListAsync(cancellationToken);

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
    public async Task<AiQuotaBucket?> GetQuotaBucketAsync(
        string providerKey,
        string? model,
        string businessKey,
        string producerKey,
        string windowType,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiQuotaBucket>()
            .Where(bucket => bucket.ProviderKey == providerKey && bucket.Model == model && bucket.BusinessKey == businessKey &&
                             bucket.ProducerKey == producerKey && bucket.WindowType == windowType && bucket.WindowStart == windowStart)
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
        await FreeSql.Select<AiQuotaBucket>().OrderByDescending(bucket => bucket.WindowStart).Limit(500).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiQuotaBucket>> ListUsageQuotaBucketsAsync(
        DateTimeOffset dateFrom,
        DateTimeOffset dateToExclusive,
        string? businessKey,
        string? producerKey,
        string? providerKey,
        string? model,
        CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AiQuotaBucket>()
            .Where(bucket => bucket.WindowType == "Day")
            .Where(bucket => bucket.WindowStart >= dateFrom && bucket.WindowStart < dateToExclusive);

        if (!string.IsNullOrWhiteSpace(businessKey))
        {
            query = query.Where(bucket => bucket.BusinessKey == businessKey);
        }

        if (!string.IsNullOrWhiteSpace(producerKey))
        {
            query = query.Where(bucket => bucket.ProducerKey == producerKey);
        }

        if (!string.IsNullOrWhiteSpace(providerKey))
        {
            query = query.Where(bucket => bucket.ProviderKey == providerKey);
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            query = query.Where(bucket => bucket.Model == model);
        }

        return await query.OrderByDescending(bucket => bucket.WindowStart).Limit(5000).ToListAsync(cancellationToken);
    }
}

using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI 调用仓储实现 partial。
/// </summary>
public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<AiInvocationLog> InsertInvocationLogAsync(AiInvocationLog log, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(log).ExecuteInsertedAsync(cancellationToken);
        log.Id = inserted.First().Id;
        return log;
    }

    /// <inheritdoc />
    public Task UpdateInvocationLogAsync(AiInvocationLog log, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AiInvocationLog>().SetSource(log).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiInvocationLog?> GetInvocationByIdempotencyAsync(string businessKey, string producerKey, string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiInvocationLog>()
            .Where(l => l.BusinessKey == businessKey && l.ProducerKey == producerKey && l.IdempotencyKey == idempotencyKey)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiInvocationLog>> ListInvocationLogsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiInvocationLog>().OrderByDescending(l => l.CreatedAt).Limit(200).ToListAsync(cancellationToken);
}

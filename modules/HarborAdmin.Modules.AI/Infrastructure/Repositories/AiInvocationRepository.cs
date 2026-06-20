using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 调用日志 FreeSql 仓储。
/// </summary>
public sealed class AiInvocationRepository(IAiDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAiDbContext>(db, unitOfWorkManager), IAiInvocationRepository
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
    public async Task<AiInvocationLog?> GetInvocationByIdempotencyAsync(
        string businessKey,
        string producerKey,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiInvocationLog>()
            .Where(log => log.BusinessKey == businessKey && log.ProducerKey == producerKey && log.IdempotencyKey == idempotencyKey)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiInvocationLog>> ListInvocationLogsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiInvocationLog>().OrderByDescending(log => log.CreatedAt).Limit(200).ToListAsync(cancellationToken);
}

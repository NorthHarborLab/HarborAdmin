using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 调用日志仓储。
/// </summary>
public interface IAiInvocationRepository
{
    /// <summary>
    /// 插入调用日志。
    /// </summary>
    Task<AiInvocationLog> InsertInvocationLogAsync(AiInvocationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新调用日志。
    /// </summary>
    Task UpdateInvocationLogAsync(AiInvocationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按幂等键获取调用日志。
    /// </summary>
    Task<AiInvocationLog?> GetInvocationByIdempotencyAsync(string businessKey, string producerKey, string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出调用日志。
    /// </summary>
    Task<IReadOnlyList<AiInvocationLog>> ListInvocationLogsAsync(CancellationToken cancellationToken = default);
}
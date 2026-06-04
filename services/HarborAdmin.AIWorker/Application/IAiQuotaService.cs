using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Snapshots;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// AI 配额服务。
/// </summary>
public interface IAiQuotaService
{
    /// <summary>
    /// 预占配额。
    /// </summary>
    Task<AiQuotaReservation> ReserveAsync(
        AiConfigSnapshot snapshot,
        AiProviderSnapshot provider,
        string model,
        AiBusinessSnapshot business,
        string producerKey,
        int estimatedTokens,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交配额。
    /// </summary>
    Task CommitAsync(AiQuotaReservation reservation, AiUsage usage, bool success, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消配额预占。
    /// </summary>
    Task CancelAsync(AiQuotaReservation reservation, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI 配额预占结果。
/// </summary>
public sealed record AiQuotaReservation(IReadOnlyList<AiQuotaBucketRef> Buckets);

/// <summary>
/// AI 配额桶引用。
/// </summary>
public sealed record AiQuotaBucketRef(string ProviderKey, string? Model, string BusinessKey, string ProducerKey, string WindowType, DateTimeOffset WindowStart);

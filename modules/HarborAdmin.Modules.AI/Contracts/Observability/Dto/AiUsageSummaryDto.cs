namespace HarborAdmin.Modules.AI.Contracts.Observability.Dto;

/// <summary>
/// AI 用量聚合明细。
/// </summary>
public sealed record AiUsageSummaryDto(
    string Label,
    string? BusinessKey,
    string? ProducerKey,
    string? ProviderKey,
    string? Model,
    DateTimeOffset? WindowStart,
    int RequestCount,
    int SuccessCount,
    int FailedCount,
    decimal SuccessRate,
    int TotalTokens,
    decimal Cost);

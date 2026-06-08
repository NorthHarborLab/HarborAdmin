namespace HarborAdmin.Modules.AI.Contracts.Observability.Dto;

/// <summary>
/// AI 用量 DTO。
/// </summary>
public sealed record AiUsageLedgerDto(
    long Id,
    string ProviderKey,
    string? Model,
    string BusinessKey,
    string ProducerKey,
    string WindowType,
    DateTimeOffset WindowStart,
    int ReservedRequests,
    int SuccessRequests,
    int FailedRequests,
    int RequestCount,
    int TotalTokens,
    decimal Cost);

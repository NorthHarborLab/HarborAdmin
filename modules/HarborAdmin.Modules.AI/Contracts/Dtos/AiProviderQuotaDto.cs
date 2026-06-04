namespace HarborAdmin.Modules.AI.Contracts.Dtos;

/// <summary>
/// AI 供应商限额 DTO。
/// </summary>
public sealed record AiProviderQuotaDto(
    long Id,
    long ProviderId,
    string? ProducerKey,
    int? RequestsPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    int? TokensPerMonth,
    decimal? MonthlyBudget,
    bool Enabled);

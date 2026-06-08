namespace HarborAdmin.Modules.AI.Contracts.Quota.Dto;

/// <summary>
/// AI 模型限额 DTO。
/// </summary>
public sealed record AiModelQuotaDto(
    long Id,
    string ProviderKey,
    string? ModelName,
    string? EndpointKey,
    string? BusinessKey,
    string? ProducerKey,
    int? RequestsPerMinute,
    int? TokensPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    decimal? MonthlyBudget,
    bool Enabled);

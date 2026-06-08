namespace HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;

/// <summary>
/// 已发布模型配额。
/// </summary>
public sealed record AiModelQuotaSnapshot(
    string ProviderKey,
    string? ModelName,
    string? BusinessKey,
    string? ProducerKey,
    int? RequestsPerMinute,
    int? TokensPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    decimal? MonthlyBudget);

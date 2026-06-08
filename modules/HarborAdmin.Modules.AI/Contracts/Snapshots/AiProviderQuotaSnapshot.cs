namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// 已发布供应商配额。
/// </summary>
public sealed record AiProviderQuotaSnapshot(
    string ProviderKey,
    string? ProducerKey,
    int? RequestsPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    int? TokensPerMonth,
    decimal? MonthlyBudget);

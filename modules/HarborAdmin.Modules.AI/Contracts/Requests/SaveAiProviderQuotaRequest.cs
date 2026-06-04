namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 供应商限额请求。
/// </summary>
public sealed record SaveAiProviderQuotaRequest(
    string? ProducerKey,
    int? RequestsPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    int? TokensPerMonth,
    decimal? MonthlyBudget,
    bool Enabled);

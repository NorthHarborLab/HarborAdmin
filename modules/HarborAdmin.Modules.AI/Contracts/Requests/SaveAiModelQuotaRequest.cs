namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 模型限额请求。
/// </summary>
public sealed record SaveAiModelQuotaRequest(
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

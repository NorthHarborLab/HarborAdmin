namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 业务供应商路由请求。
/// </summary>
public sealed record SaveAiBusinessProviderRouteRequest(
    string ProviderKey,
    string? ModelOverride,
    int Priority,
    bool Enabled,
    string? ProviderOptionsJson,
    string? OpenRouterOptionsJson);


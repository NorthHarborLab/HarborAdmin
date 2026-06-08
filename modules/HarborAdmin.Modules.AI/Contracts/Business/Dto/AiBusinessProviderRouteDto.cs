namespace HarborAdmin.Modules.AI.Contracts.Business.Dto;

/// <summary>
/// AI 业务供应商路由 DTO。
/// </summary>
public sealed record AiBusinessProviderRouteDto(
    long Id,
    long BusinessId,
    string ProviderKey,
    string? ModelOverride,
    int Priority,
    bool Enabled,
    string? ProviderOptionsJson,
    string? OpenRouterOptionsJson);


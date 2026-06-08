namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// 已发布业务路由。
/// </summary>
public sealed record AiBusinessRouteSnapshot(
    string ProviderKey,
    string? ModelOverride,
    int Priority,
    string? ProviderOptionsJson,
    string? OpenRouterOptionsJson);

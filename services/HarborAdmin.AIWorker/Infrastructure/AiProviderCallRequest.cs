using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// AI 供应商调用请求。
/// </summary>
public sealed record AiProviderCallRequest(
    AiProviderSnapshot Provider,
    string Model,
    IReadOnlyList<AiMessage> Messages,
    bool Streaming,
    string InvocationId,
    string CorrelationId,
    int ReleaseVersion,
    string? ResolvedSecret,
    AiOutputOptions? OutputOptions = null,
    AiToolOptions? ToolOptions = null,
    AiProviderOptions? ProviderOptions = null,
    string? RouteProviderOptionsJson = null,
    string? OpenRouterOptionsJson = null);

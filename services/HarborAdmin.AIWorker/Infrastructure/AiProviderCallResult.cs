using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// AI 供应商调用结果。
/// </summary>
public sealed record AiProviderCallResult(
    string Content,
    AiUsage Usage,
    string? ProviderRequestId,
    string? FinishReason = null,
    string? UpstreamProvider = null,
    int ToolCallCount = 0,
    string? ReasoningContent = null);

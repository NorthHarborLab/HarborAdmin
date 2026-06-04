namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 流式事件。
/// </summary>
public sealed record AiStreamEvent(
    string Type,
    string InvocationId,
    string CorrelationId,
    int Sequence,
    int ReleaseVersion,
    string? Delta = null,
    string? ProviderKey = null,
    string? Model = null,
    AiUsage? Usage = null,
    AiReference? Reference = null,
    AiToolCall? ToolCall = null,
    string? FinishReason = null,
    string? ProviderRequestId = null,
    string? UpstreamProvider = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

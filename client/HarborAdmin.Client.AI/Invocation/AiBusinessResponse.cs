namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 业务响应。
/// </summary>
public sealed record AiBusinessResponse(
    bool Success,
    string InvocationId,
    string CorrelationId,
    string Status,
    int ReleaseVersion,
    string? Content = null,
    string? ProviderKey = null,
    string? Model = null,
    AiUsage? Usage = null,
    IReadOnlyList<AiReference>? References = null,
    IReadOnlyDictionary<string, string>? Context = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ReasoningContent = null);

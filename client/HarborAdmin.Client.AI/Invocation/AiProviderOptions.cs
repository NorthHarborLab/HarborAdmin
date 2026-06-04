namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 供应商选项。
/// </summary>
public sealed record AiProviderOptions(
    double? Temperature = null,
    double? TopP = null,
    int? MaxTokens = null,
    IReadOnlyList<string>? Stop = null,
    string? ReasoningEffort = null,
    string? ExtraBodyJson = null);



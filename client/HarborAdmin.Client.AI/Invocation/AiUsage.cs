namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 用量。
/// </summary>
public sealed record AiUsage(
    int PromptTokens = 0,
    int CompletionTokens = 0,
    int TotalTokens = 0,
    int ReasoningTokens = 0,
    int CachedTokens = 0,
    int NativePromptTokens = 0,
    int NativeCompletionTokens = 0,
    decimal Cost = 0);



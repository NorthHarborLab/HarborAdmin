namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// 已发布模型。
/// </summary>
public sealed record AiProviderModelSnapshot(
    string ModelName,
    bool IsDefault,
    bool SupportsStreaming,
    bool SupportsVision,
    bool SupportsTools,
    bool SupportsStructuredOutput,
    bool SupportsJsonMode,
    int? ContextWindow,
    int? MaxOutputTokens,
    decimal? InputPrice,
    decimal? OutputPrice,
    decimal? CachedInputPrice,
    decimal? ReasoningPrice);

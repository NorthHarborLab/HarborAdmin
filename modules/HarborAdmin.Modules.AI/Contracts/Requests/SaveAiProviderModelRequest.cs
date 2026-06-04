namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 供应商模型请求。
/// </summary>
public sealed record SaveAiProviderModelRequest(
    string ModelName,
    string? DisplayName,
    bool IsDefault,
    bool Enabled,
    bool SupportsStreaming,
    string? InputModalities,
    string? OutputModalities,
    bool SupportsVision,
    bool SupportsTools,
    bool SupportsStructuredOutput,
    bool SupportsJsonMode,
    bool SupportsReasoning,
    int? ContextWindow,
    int? MaxOutputTokens,
    decimal? InputPrice,
    decimal? OutputPrice,
    decimal? CachedInputPrice,
    decimal? ReasoningPrice,
    int SortOrder);


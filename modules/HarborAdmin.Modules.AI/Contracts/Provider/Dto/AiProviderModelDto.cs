namespace HarborAdmin.Modules.AI.Contracts.Provider.Dto;

/// <summary>
/// AI 供应商模型 DTO。
/// </summary>
public sealed record AiProviderModelDto(
    long Id,
    long ProviderId,
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


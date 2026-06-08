namespace HarborAdmin.Modules.AI.Contracts.Business.Dto;

/// <summary>
/// AI 业务 DTO。
/// </summary>
public sealed record AiBusinessDto(
    long Id,
    string BusinessKey,
    string Name,
    string? AllowedProducerKeys,
    string? SigningSecretRef,
    string? CallbackTopic,
    string? PromptKey,
    string? KnowledgeKeys,
    bool EnableStreaming,
    bool AllowKnowledgeTextAppend,
    bool AllowKnowledgeTextOverride,
    int MaxContextTokens,
    string ContextOverflowStrategy,
    string FailureStrategy,
    bool AllowModelOverride,
    bool AllowPromptOverride,
    bool AllowKnowledgeText,
    bool AllowProviderOptionsOverride,
    bool AllowToolOptionsOverride,
    bool Enabled,
    string? OutputFormat,
    string? OutputJsonSchema,
    bool OutputStrict,
    bool OutputValidateAndRetry,
    int OutputMaxRetryCount,
    string? ToolOptionsJson,
    int MaxToolRounds,
    string? ProviderOptionsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AiBusinessProviderRouteDto> Routes);

namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// AI 已发布配置快照。
/// </summary>
public sealed record AiConfigSnapshot(
    int Version,
    IReadOnlyList<AiProviderSnapshot> Providers,
    IReadOnlyList<AiBusinessSnapshot> Businesses,
    IReadOnlyList<AiPromptSnapshot> Prompts,
    IReadOnlyList<AiKnowledgeSnapshot> KnowledgeBases,
    IReadOnlyList<AiProviderQuotaSnapshot> ProviderQuotas,
    IReadOnlyList<AiModelQuotaSnapshot> ModelQuotas);

/// <summary>
/// 已发布供应商。
/// </summary>
public sealed record AiProviderSnapshot(
    string ProviderKey,
    string DisplayName,
    string AdapterType,
    string BaseUrl,
    string? SecretRef,
    int SecretVersion,
    string? DefaultHeadersJson,
    string? DefaultBodyJson,
    bool SupportsStreaming,
    int TimeoutSeconds,
    int MaxRetryCount,
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerBreakSeconds,
    IReadOnlyList<AiProviderModelSnapshot> Models);

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

/// <summary>
/// 已发布业务。
/// </summary>
public sealed record AiBusinessSnapshot(
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
    string? OutputFormat,
    string? OutputJsonSchema,
    bool OutputStrict,
    bool OutputValidateAndRetry,
    int OutputMaxRetryCount,
    string? ToolOptionsJson,
    int MaxToolRounds,
    string? ProviderOptionsJson,
    IReadOnlyList<AiBusinessRouteSnapshot> Routes);

/// <summary>
/// 已发布业务路由。
/// </summary>
public sealed record AiBusinessRouteSnapshot(
    string ProviderKey,
    string? ModelOverride,
    int Priority,
    string? ProviderOptionsJson,
    string? OpenRouterOptionsJson);

/// <summary>
/// 已发布 Prompt。
/// </summary>
public sealed record AiPromptSnapshot(
    string PromptKey,
    int Version,
    string SystemPromptMarkdown,
    string UserPromptMarkdown,
    string? VariablesJson);

/// <summary>
/// 已发布知识库。
/// </summary>
public sealed record AiKnowledgeSnapshot(
    string KnowledgeKey,
    string Name,
    string ContentMarkdown,
    string RetrievalType,
    string? RetrievalOptionsJson,
    bool AppendReferences);

/// <summary>
/// 已发布供应商配额。
/// </summary>
public sealed record AiProviderQuotaSnapshot(
    string ProviderKey,
    string? ProducerKey,
    int? RequestsPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    int? TokensPerMonth,
    decimal? MonthlyBudget);

/// <summary>
/// 已发布模型配额。
/// </summary>
public sealed record AiModelQuotaSnapshot(
    string ProviderKey,
    string? ModelName,
    string? BusinessKey,
    string? ProducerKey,
    int? RequestsPerMinute,
    int? TokensPerMinute,
    int? RequestsPerDay,
    int? TokensPerDay,
    decimal? MonthlyBudget);

namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 业务请求。
/// </summary>
public sealed record SaveAiBusinessRequest(
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
    IReadOnlyList<SaveAiBusinessProviderRouteRequest> Routes);

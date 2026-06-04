namespace HarborAdmin.Client.AI.Invocation;

/// <summary>
/// AI 业务请求。
/// </summary>
public sealed record AiBusinessRequest(
    string BusinessKey,
    string? InvocationId = null,
    string? ProducerKey = null,
    string? IdempotencyKey = null,
    string? Model = null,
    string? PromptOverride = null,
    IReadOnlyDictionary<string, string>? PromptVariables = null,
    IReadOnlyList<AiMessage>? Messages = null,
    string? KnowledgeText = null,
    string? KnowledgeTextMode = null,
    IReadOnlyDictionary<string, string>? Context = null,
    IReadOnlyList<AiAttachment>? Attachments = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    string? CallbackName = null,
    string? Input = null,
    string? CorrelationId = null,
    AiOutputOptions? OutputOptions = null,
    AiProviderOptions? ProviderOptions = null);

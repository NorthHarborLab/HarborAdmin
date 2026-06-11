using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.Modules.AI.Contracts.Chat.Request;

/// <summary>
/// AI 聊天流式请求。
/// </summary>
public sealed record AiChatStreamRequest(
    string BusinessKey,
    string? ProducerKey = null,
    string? Model = null,
    string? PromptOverride = null,
    IReadOnlyDictionary<string, string>? PromptVariables = null,
    IReadOnlyList<AiMessage>? Messages = null,
    string? KnowledgeText = null,
    string? KnowledgeTextMode = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    string? Input = null,
    long? ConversationId = null);

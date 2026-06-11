namespace HarborAdmin.Modules.AI.Contracts.Conversation.Dto;

/// <summary>
/// AI 聊天会话列表项 DTO。
/// </summary>
public sealed record AiConversationDto(
    string Id,
    string Title,
    string BusinessKey,
    string? ProducerKey,
    string? Model,
    string UpdateTime);

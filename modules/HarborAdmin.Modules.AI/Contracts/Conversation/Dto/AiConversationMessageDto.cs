namespace HarborAdmin.Modules.AI.Contracts.Conversation.Dto;

/// <summary>
/// AI 聊天会话消息 DTO。
/// </summary>
public sealed record AiConversationMessageDto(
    string Id,
    string Role,
    string Content,
    int Sequence,
    string? InvocationId,
    bool IsError);

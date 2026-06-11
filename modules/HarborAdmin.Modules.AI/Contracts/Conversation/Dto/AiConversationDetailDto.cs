namespace HarborAdmin.Modules.AI.Contracts.Conversation.Dto;

/// <summary>
/// AI 聊天会话详情 DTO。
/// </summary>
public sealed record AiConversationDetailDto(
    string Id,
    string Title,
    string BusinessKey,
    string? ProducerKey,
    string? Model,
    string? PromptVariablesJson,
    string UpdateTime,
    IReadOnlyList<AiConversationMessageDto> Messages);

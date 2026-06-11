namespace HarborAdmin.Modules.AI.Contracts.Chat.Dto;

/// <summary>
/// AI 聊天可选业务。
/// </summary>
public sealed record AiChatBusinessOptionDto(
    string BusinessKey,
    string Name,
    string? AllowedProducerKeys);

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 聊天会话消息。
/// </summary>
[Index("idx_ai_conversation_message_conv_seq", $"{nameof(ConversationId)},{nameof(Sequence)}")]
public sealed class AiConversationMessage : EntityBase
{
    /// <summary>
    /// 所属会话 ID。
    /// </summary>
    public long ConversationId { get; set; }

    /// <summary>
    /// 消息角色。
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 消息正文。
    /// </summary>
    [Column(StringLength = -1)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 思考过程正文。
    /// </summary>
    [Column(StringLength = -1)]
    public string? ReasoningContent { get; set; }

    /// <summary>
    /// 会话内顺序。
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// 关联调用 ID。
    /// </summary>
    public string? InvocationId { get; set; }

    /// <summary>
    /// 是否为错误回复。
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// 所属会话。
    /// </summary>
    [Navigate(nameof(ConversationId))]
    public AiConversation? Conversation { get; set; }
}

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 聊天会话。
/// </summary>
[Index("idx_ai_conversation_user_updated", $"{nameof(UserId)},{nameof(UpdatedAt)}")]
public sealed class AiConversation : AuditableEntity
{
    /// <summary>
    /// 所属用户 ID。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 会话标题。
    /// </summary>
    public string Title { get; set; } = "新对话";

    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    public string? ProducerKey { get; set; }

    /// <summary>
    /// 模型覆盖。
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Prompt 变量 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? PromptVariablesJson { get; set; }

    /// <summary>
    /// 会话消息列表。
    /// </summary>
    [Navigate(nameof(AiConversationMessage.ConversationId))]
    public List<AiConversationMessage> Messages { get; set; } = [];
}

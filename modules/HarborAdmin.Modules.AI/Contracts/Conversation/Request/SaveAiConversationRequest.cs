using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Conversation.Request;

/// <summary>
/// 保存 AI 聊天会话请求。
/// </summary>
public sealed class SaveAiConversationRequest
{
    /// <summary>
    /// 会话标题。
    /// </summary>
    [MaxLength(120)]
    public string? Title { get; set; }

    /// <summary>
    /// 业务 Key。
    /// </summary>
    [MaxLength(64)]
    public string? BusinessKey { get; set; }

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    [MaxLength(64)]
    public string? ProducerKey { get; set; }

    /// <summary>
    /// 模型覆盖。
    /// </summary>
    [MaxLength(120)]
    public string? Model { get; set; }

    /// <summary>
    /// Prompt 变量 JSON。
    /// </summary>
    public string? PromptVariablesJson { get; set; }
}

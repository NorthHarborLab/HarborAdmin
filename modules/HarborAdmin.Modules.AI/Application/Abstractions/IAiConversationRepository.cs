using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 会话仓储。
/// </summary>
public interface IAiConversationRepository
{
    /// <summary>
    /// 分页列出用户会话。
    /// </summary>
    Task<(IReadOnlyList<AiConversation> Items, long Total)> ListConversationsAsync(long userId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户会话。
    /// </summary>
    Task<AiConversation?> GetConversationAsync(long userId, long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户会话及消息。
    /// </summary>
    Task<AiConversation?> GetConversationDetailAsync(long userId, long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入会话。
    /// </summary>
    Task<AiConversation> InsertConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新会话。
    /// </summary>
    Task UpdateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除会话及消息。
    /// </summary>
    Task DeleteConversationAsync(long userId, long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入会话消息。
    /// </summary>
    Task<AiConversationMessage> InsertConversationMessageAsync(AiConversationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话下一条消息序号。
    /// </summary>
    Task<int> GetNextConversationMessageSequenceAsync(long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计会话消息数量。
    /// </summary>
    Task<long> CountConversationMessagesAsync(long conversationId, CancellationToken cancellationToken = default);
}
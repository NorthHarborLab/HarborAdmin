using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 会话 FreeSql 仓储。
/// </summary>
public sealed class AiConversationRepository(IAiDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAiDbContext>(db, unitOfWorkManager), IAiConversationRepository
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<AiConversation> Items, long Total)> ListConversationsAsync(
        long userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AiConversation>().Where(conversation => conversation.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(conversation => conversation.UpdatedAt ?? conversation.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<AiConversation?> GetConversationAsync(long userId, long conversationId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConversation>()
            .Where(conversation => conversation.Id == conversationId && conversation.UserId == userId)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConversation?> GetConversationDetailAsync(long userId, long conversationId, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConversation>()
            .Where(conversation => conversation.Id == conversationId && conversation.UserId == userId)
            .IncludeMany(conversation => conversation.Messages, then => then.OrderBy(message => message.Sequence))
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConversation> InsertConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(conversation).ExecuteInsertedAsync(cancellationToken);
        conversation.Id = inserted.First().Id;
        return conversation;
    }

    /// <inheritdoc />
    public Task UpdateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default) =>
        FreeSql.Update<AiConversation>().SetSource(conversation).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteConversationAsync(long userId, long conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await GetConversationAsync(userId, conversationId, cancellationToken);
        if (conversation is null)
        {
            return;
        }

        await FreeSql.Delete<AiConversationMessage>()
            .Where(message => message.ConversationId == conversationId)
            .ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AiConversation>()
            .Where(item => item.Id == conversationId && item.UserId == userId)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiConversationMessage> InsertConversationMessageAsync(
        AiConversationMessage message,
        CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(message).ExecuteInsertedAsync(cancellationToken);
        message.Id = inserted.First().Id;
        return message;
    }

    /// <inheritdoc />
    public async Task<int> GetNextConversationMessageSequenceAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        var maxSequence = await FreeSql.Select<AiConversationMessage>()
            .Where(message => message.ConversationId == conversationId)
            .MaxAsync(message => message.Sequence, cancellationToken);
        return maxSequence + 1;
    }

    /// <inheritdoc />
    public Task<long> CountConversationMessagesAsync(long conversationId, CancellationToken cancellationToken = default) =>
        FreeSql.Select<AiConversationMessage>()
            .Where(message => message.ConversationId == conversationId)
            .CountAsync(cancellationToken);
}

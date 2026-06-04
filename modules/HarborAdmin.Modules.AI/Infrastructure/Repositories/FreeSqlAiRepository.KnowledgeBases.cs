using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AiKnowledgeBase>> ListKnowledgeBasesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiKnowledgeBase>().OrderBy(k => k.KnowledgeKey).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiKnowledgeBase>> ListEnabledKnowledgeBasesAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var normalized = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToArray();
        return await FreeSql.Select<AiKnowledgeBase>()
            .Where(k => k.Enabled && normalized.Contains(k.KnowledgeKey))
            .OrderBy(k => k.KnowledgeKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiKnowledgeBase?> GetKnowledgeBaseAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiKnowledgeBase>().Where(k => k.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiKnowledgeBase> SaveKnowledgeBaseAsync(AiKnowledgeBase knowledgeBase, CancellationToken cancellationToken = default)
    {
        if (knowledgeBase.Id == 0)
        {
            var inserted = await FreeSql.Insert(knowledgeBase).ExecuteInsertedAsync(cancellationToken);
            knowledgeBase.Id = inserted.First().Id;
            return knowledgeBase;
        }

        await FreeSql.Update<AiKnowledgeBase>().SetSource(knowledgeBase).ExecuteAffrowsAsync(cancellationToken);
        return knowledgeBase;
    }

    /// <inheritdoc />
    public Task DeleteKnowledgeBaseAsync(long id, CancellationToken cancellationToken = default) =>
        FreeSql.Delete<AiKnowledgeBase>().Where(k => k.Id == id).ExecuteAffrowsAsync(cancellationToken);
}

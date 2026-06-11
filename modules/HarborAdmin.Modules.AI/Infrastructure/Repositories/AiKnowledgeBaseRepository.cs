using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 知识库实体 CRUD 仓储。
/// </summary>
public sealed class AiKnowledgeBaseRepository(IAiDbContext db, DbEntityRegistry entityRegistry)
    : FreeSqlEntityRepository<AiKnowledgeBase, IAiDbContext>(db, entityRegistry), IAiKnowledgeBaseRepository
{
    /// <inheritdoc />
    protected override ISelect<AiKnowledgeBase> BuildListQuery(ISelect<AiKnowledgeBase> query) =>
        query.OrderBy(knowledgeBase => knowledgeBase.KnowledgeKey);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiKnowledgeBase>> ListEnabledKnowledgeBasesAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var normalized = keys.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim()).ToArray();
        return await FreeSql.Select<AiKnowledgeBase>()
            .Where(knowledgeBase => knowledgeBase.Enabled && normalized.Contains(knowledgeBase.KnowledgeKey))
            .OrderBy(knowledgeBase => knowledgeBase.KnowledgeKey)
            .ToListAsync(cancellationToken);
    }
}

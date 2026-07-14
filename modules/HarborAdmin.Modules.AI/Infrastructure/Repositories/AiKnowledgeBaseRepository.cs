using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 知识库实体 CRUD 仓储。
/// </summary>
public sealed class AiKnowledgeBaseRepository(
    IAiDbContext db,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AiKnowledgeBase, IAiDbContext>(db, entityRegistry, unitOfWorkManager), IAiKnowledgeBaseRepository
{
    /// <inheritdoc />
    public Task<bool> KnowledgeKeyExistsAsync(string knowledgeKey, long? excludeId, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AiKnowledgeBase>().Where(entity => entity.KnowledgeKey == knowledgeKey);
        if (excludeId.HasValue)
        {
            query = query.Where(entity => entity.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

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

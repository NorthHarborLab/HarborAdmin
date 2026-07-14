using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

/// <summary>
/// AI 知识库实体 CRUD 仓储。
/// </summary>
public interface IAiKnowledgeBaseRepository : IHarborCrudRepository<AiKnowledgeBase>
{
    /// <summary>
    /// 加载启用的知识库。
    /// </summary>
    Task<IReadOnlyList<AiKnowledgeBase>> ListEnabledKnowledgeBasesAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断知识库 Key 是否已被其他记录使用。
    /// </summary>
    Task<bool> KnowledgeKeyExistsAsync(string knowledgeKey, long? excludeId, CancellationToken cancellationToken = default);
}

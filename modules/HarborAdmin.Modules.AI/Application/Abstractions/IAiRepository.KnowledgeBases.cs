using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

public partial interface IAiRepository
{
    /// <summary>
    /// 列出知识库。
    /// </summary>
    Task<IReadOnlyList<AiKnowledgeBase>> ListKnowledgeBasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出启用知识库。
    /// </summary>
    Task<IReadOnlyList<AiKnowledgeBase>> ListEnabledKnowledgeBasesAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取知识库。
    /// </summary>
    Task<AiKnowledgeBase?> GetKnowledgeBaseAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存知识库。
    /// </summary>
    Task<AiKnowledgeBase> SaveKnowledgeBaseAsync(AiKnowledgeBase knowledgeBase, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除知识库。
    /// </summary>
    Task DeleteKnowledgeBaseAsync(long id, CancellationToken cancellationToken = default);
}

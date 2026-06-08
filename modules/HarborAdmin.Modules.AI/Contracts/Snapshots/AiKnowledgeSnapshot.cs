namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// 已发布知识库。
/// </summary>
public sealed record AiKnowledgeSnapshot(
    string KnowledgeKey,
    string Name,
    string ContentMarkdown,
    string RetrievalType,
    string? RetrievalOptionsJson,
    bool AppendReferences);

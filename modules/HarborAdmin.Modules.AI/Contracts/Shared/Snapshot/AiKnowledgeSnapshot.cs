namespace HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;

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

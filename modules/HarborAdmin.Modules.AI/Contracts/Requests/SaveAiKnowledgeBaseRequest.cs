namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 知识库请求。
/// </summary>
public sealed record SaveAiKnowledgeBaseRequest(
    string KnowledgeKey,
    string Name,
    string ContentMarkdown,
    string RetrievalType,
    string? RetrievalOptionsJson,
    bool AppendReferences,
    bool Enabled);


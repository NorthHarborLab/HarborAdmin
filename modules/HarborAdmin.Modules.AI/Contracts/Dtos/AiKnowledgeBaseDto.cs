namespace HarborAdmin.Modules.AI.Contracts.Dtos;

/// <summary>
/// AI 知识库 DTO。
/// </summary>
public sealed record AiKnowledgeBaseDto(
    long Id,
    string KnowledgeKey,
    string Name,
    string ContentMarkdown,
    string RetrievalType,
    string? RetrievalOptionsJson,
    bool AppendReferences,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);


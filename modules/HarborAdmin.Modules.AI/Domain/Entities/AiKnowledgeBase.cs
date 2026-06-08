using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 知识库。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_knowledge_key", nameof(KnowledgeKey), true)]
public sealed class AiKnowledgeBase : AuditableEntity
{
    /// <summary>
    /// 知识库 Key。
    /// </summary>
    public string KnowledgeKey { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Markdown 正文。
    /// </summary>
    [Column(StringLength = -1)]
    public string ContentMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 检索类型。
    /// </summary>
    public string RetrievalType { get; set; } = "Static";

    /// <summary>
    /// 检索参数 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? RetrievalOptionsJson { get; set; }

    /// <summary>
    /// 是否追加引用。
    /// </summary>
    public bool AppendReferences { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

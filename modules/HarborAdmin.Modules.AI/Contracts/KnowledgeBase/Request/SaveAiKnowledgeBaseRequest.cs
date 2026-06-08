using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Request;

/// <summary>
/// 保存 AI 知识库请求。
/// </summary>
public sealed class SaveAiKnowledgeBaseRequest
{
    /// <summary>
    /// 知识库 Key。
    /// </summary>
    [Required(ErrorMessage = "知识库 Key 不能为空。")]
    [MaxLength(64)]
    public string KnowledgeKey { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [Required(ErrorMessage = "知识库名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 内容（Markdown）。
    /// </summary>
    [Required(ErrorMessage = "知识库内容不能为空。")]
    public string ContentMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 检索类型。
    /// </summary>
    [Required(ErrorMessage = "检索类型不能为空。")]
    [MaxLength(64)]
    public string RetrievalType { get; set; } = string.Empty;

    /// <summary>
    /// 检索选项 JSON。
    /// </summary>
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

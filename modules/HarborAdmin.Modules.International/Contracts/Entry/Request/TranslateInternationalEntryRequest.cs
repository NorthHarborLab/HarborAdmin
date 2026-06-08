using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.International.Contracts.Entry.Request;

/// <summary>
/// AI 翻译国际化条目请求。
/// </summary>
public sealed class TranslateInternationalEntryRequest
{
    /// <summary>
    /// 目标语言列表。
    /// </summary>
    public IReadOnlyList<string>? TargetLocales { get; set; }

    /// <summary>
    /// 指定模型。
    /// </summary>
    [MaxLength(120)]
    public string? Model { get; set; }

    /// <summary>
    /// 提示词覆盖。
    /// </summary>
    [MaxLength(4000)]
    public string? PromptOverride { get; set; }

    /// <summary>
    /// 知识库文本。
    /// </summary>
    [MaxLength(8000)]
    public string? KnowledgeText { get; set; }

    /// <summary>
    /// 知识库文本模式。
    /// </summary>
    [MaxLength(64)]
    public string? KnowledgeTextMode { get; set; }
}

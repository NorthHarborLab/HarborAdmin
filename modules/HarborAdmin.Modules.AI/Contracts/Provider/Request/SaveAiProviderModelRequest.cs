using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Provider.Request;

/// <summary>
/// 保存 AI 供应商模型请求。
/// </summary>
public sealed class SaveAiProviderModelRequest
{
    /// <summary>
    /// 模型名称。
    /// </summary>
    [Required(ErrorMessage = "模型名称不能为空。")]
    [MaxLength(120)]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    [MaxLength(120)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 是否默认。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 是否支持流式。
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// 输入模态，逗号分隔。
    /// </summary>
    [MaxLength(200)]
    public string? InputModalities { get; set; }

    /// <summary>
    /// 输出模态，逗号分隔。
    /// </summary>
    [MaxLength(200)]
    public string? OutputModalities { get; set; }

    /// <summary>
    /// 是否支持视觉输入。
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// 是否支持工具调用。
    /// </summary>
    public bool SupportsTools { get; set; }

    /// <summary>
    /// 是否支持结构化输出。
    /// </summary>
    public bool SupportsStructuredOutput { get; set; }

    /// <summary>
    /// 是否支持 JSON 模式。
    /// </summary>
    public bool SupportsJsonMode { get; set; }

    /// <summary>
    /// 是否支持推理模式。
    /// </summary>
    public bool SupportsReasoning { get; set; }

    /// <summary>
    /// 上下文窗口。
    /// </summary>
    public int? ContextWindow { get; set; }

    /// <summary>
    /// 最大输出 Token。
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 输入单价。
    /// </summary>
    public decimal? InputPrice { get; set; }

    /// <summary>
    /// 输出单价。
    /// </summary>
    public decimal? OutputPrice { get; set; }

    /// <summary>
    /// 缓存输入单价。
    /// </summary>
    public decimal? CachedInputPrice { get; set; }

    /// <summary>
    /// 推理单价。
    /// </summary>
    public decimal? ReasoningPrice { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }
}

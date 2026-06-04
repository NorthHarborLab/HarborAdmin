using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using FreeSql.DataAnnotations;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 供应商模型。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_provider_model", "ProviderId,ModelName", true)]
public class AiProviderModel : EntityBase
{
    /// <summary>
    /// 供应商主键。
    /// </summary>
    public long ProviderId { get; set; }

    /// <summary>
    /// 模型名称。
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
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
    public string? InputModalities { get; set; }

    /// <summary>
    /// 输出模态，逗号分隔。
    /// </summary>
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
    /// 是否支持推理 token。
    /// </summary>
    public bool SupportsReasoning { get; set; }

    /// <summary>
    /// 上下文窗口 token。
    /// </summary>
    public int? ContextWindow { get; set; }

    /// <summary>
    /// 最大输出 token。
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 输入价格。
    /// </summary>
    public decimal? InputPrice { get; set; }

    /// <summary>
    /// 输出价格。
    /// </summary>
    public decimal? OutputPrice { get; set; }

    /// <summary>
    /// 缓存输入价格。
    /// </summary>
    public decimal? CachedInputPrice { get; set; }

    /// <summary>
    /// 推理 token 价格。
    /// </summary>
    public decimal? ReasoningPrice { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}


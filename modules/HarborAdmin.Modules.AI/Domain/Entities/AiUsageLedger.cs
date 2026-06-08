using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 用量台账。
/// </summary>
[DbKey("AdminDb")]
public sealed class AiUsageLedger : EntityBase
{
    /// <summary>
    /// 供应商 Key。
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型。
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 用量日期。
    /// </summary>
    public DateOnly UsageDate { get; set; }

    /// <summary>
    /// 请求数。
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// 成功数。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数。
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Prompt Token。
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Completion Token。
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总 Token。
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 原生 Prompt Token。
    /// </summary>
    public int NativePromptTokens { get; set; }

    /// <summary>
    /// 原生 Completion Token。
    /// </summary>
    public int NativeCompletionTokens { get; set; }

    /// <summary>
    /// 成本。
    /// </summary>
    public decimal Cost { get; set; }
}


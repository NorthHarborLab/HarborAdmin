using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 调用日志。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_invocation_id", nameof(InvocationId), true)]
[Index("ux_ai_invocation_idempotency", $"{nameof(BusinessKey)},{nameof(ProducerKey)},{nameof(IdempotencyKey)}", true)]
public sealed class AiInvocationLog : EntityBase
{
    /// <summary>
    /// 调用 ID。
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>
    /// 关联 ID。
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    public string ProducerKey { get; set; } = string.Empty;

    /// <summary>
    /// 幂等 Key。
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// 发布版本。
    /// </summary>
    public int ReleaseVersion { get; set; }

    /// <summary>
    /// 供应商 Key。
    /// </summary>
    public string? ProviderKey { get; set; }

    /// <summary>
    /// 请求模型。
    /// </summary>
    public string? RequestedModel { get; set; }

    /// <summary>
    /// 实际模型。
    /// </summary>
    public string? ActualModel { get; set; }

    /// <summary>
    /// 是否流式。
    /// </summary>
    public bool Streaming { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public string Status { get; set; } = "Pending";

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
    /// 推理 Token。
    /// </summary>
    public int ReasoningTokens { get; set; }

    /// <summary>
    /// 缓存 Token。
    /// </summary>
    public int CachedTokens { get; set; }

    /// <summary>
    /// 耗时毫秒。
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// 错误码。
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 错误分类。
    /// </summary>
    public string? ErrorCategory { get; set; }

    /// <summary>
    /// 错误消息。
    /// </summary>
    [Column(StringLength = -1)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 回退轨迹。
    /// </summary>
    [Column(StringLength = -1)]
    public string? FallbackTrace { get; set; }

    /// <summary>
    /// 上下文长度。
    /// </summary>
    public int ContextLength { get; set; }

    /// <summary>
    /// 供应商请求 ID。
    /// </summary>
    public string? ProviderRequestId { get; set; }

    /// <summary>
    /// 停止原因。
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// 成本。
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// 原生 Prompt Token。
    /// </summary>
    public int NativePromptTokens { get; set; }

    /// <summary>
    /// 原生 Completion Token。
    /// </summary>
    public int NativeCompletionTokens { get; set; }

    /// <summary>
    /// 上游供应商。
    /// </summary>
    public string? UpstreamProvider { get; set; }

    /// <summary>
    /// 输出格式。
    /// </summary>
    public string? OutputFormat { get; set; }

    /// <summary>
    /// 工具调用次数。
    /// </summary>
    public int ToolCallCount { get; set; }

    /// <summary>
    /// 请求摘要。
    /// </summary>
    public string? RequestHash { get; set; }

    /// <summary>
    /// 响应摘要。
    /// </summary>
    public string? ResponseHash { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}


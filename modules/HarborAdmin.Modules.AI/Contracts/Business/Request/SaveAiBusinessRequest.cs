using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Business.Request;

/// <summary>
/// 保存 AI 业务请求。
/// </summary>
public sealed class SaveAiBusinessRequest
{
    /// <summary>
    /// 业务 Key。
    /// </summary>
    [Required(ErrorMessage = "业务 Key 不能为空。")]
    [MaxLength(64)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [Required(ErrorMessage = "业务名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 允许的调用方 Key，逗号分隔。
    /// </summary>
    [MaxLength(500)]
    public string? AllowedProducerKeys { get; set; }

    /// <summary>
    /// 服务间签名密钥引用。
    /// </summary>
    [MaxLength(128)]
    public string? SigningSecretRef { get; set; }

    /// <summary>
    /// 回调 Topic。
    /// </summary>
    [MaxLength(120)]
    public string? CallbackTopic { get; set; }

    /// <summary>
    /// 默认 Prompt Key。
    /// </summary>
    [MaxLength(64)]
    public string? PromptKey { get; set; }

    /// <summary>
    /// 绑定知识库 Key，逗号分隔。
    /// </summary>
    [MaxLength(500)]
    public string? KnowledgeKeys { get; set; }

    /// <summary>
    /// 是否允许流式。
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    /// 是否允许追加调用方知识文本。
    /// </summary>
    public bool AllowKnowledgeTextAppend { get; set; }

    /// <summary>
    /// 是否允许覆盖默认知识库。
    /// </summary>
    public bool AllowKnowledgeTextOverride { get; set; }

    /// <summary>
    /// 最大上下文 Token。
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "最大上下文 Token 不合法。")]
    public int MaxContextTokens { get; set; }

    /// <summary>
    /// 上下文超限策略。
    /// </summary>
    [Required(ErrorMessage = "上下文超限策略不能为空。")]
    [MaxLength(64)]
    public string ContextOverflowStrategy { get; set; } = "Reject";

    /// <summary>
    /// 失败策略。
    /// </summary>
    [Required(ErrorMessage = "失败策略不能为空。")]
    [MaxLength(64)]
    public string FailureStrategy { get; set; } = "ReturnError";

    /// <summary>
    /// 是否允许调用方覆盖模型。
    /// </summary>
    public bool AllowModelOverride { get; set; }

    /// <summary>
    /// 是否允许调用方覆盖 Prompt。
    /// </summary>
    public bool AllowPromptOverride { get; set; }

    /// <summary>
    /// 是否允许调用方传入知识文本。
    /// </summary>
    public bool AllowKnowledgeText { get; set; }

    /// <summary>
    /// 是否允许调用方覆盖供应商选项。
    /// </summary>
    public bool AllowProviderOptionsOverride { get; set; }

    /// <summary>
    /// 是否允许调用方覆盖工具选项。
    /// </summary>
    public bool AllowToolOptionsOverride { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 输出格式。
    /// </summary>
    [MaxLength(64)]
    public string? OutputFormat { get; set; }

    /// <summary>
    /// 输出 JSON Schema。
    /// </summary>
    public string? OutputJsonSchema { get; set; }

    /// <summary>
    /// 是否严格输出。
    /// </summary>
    public bool OutputStrict { get; set; }

    /// <summary>
    /// 是否校验并重试输出。
    /// </summary>
    public bool OutputValidateAndRetry { get; set; }

    /// <summary>
    /// 输出最大重试次数。
    /// </summary>
    [Range(0, 100, ErrorMessage = "输出最大重试次数不合法。")]
    public int OutputMaxRetryCount { get; set; }

    /// <summary>
    /// 工具选项 JSON。
    /// </summary>
    public string? ToolOptionsJson { get; set; }

    /// <summary>
    /// 最大工具轮次。
    /// </summary>
    [Range(0, 1000, ErrorMessage = "最大工具轮次不合法。")]
    public int MaxToolRounds { get; set; }

    /// <summary>
    /// 供应商选项 JSON。
    /// </summary>
    public string? ProviderOptionsJson { get; set; }

    /// <summary>
    /// 供应商路由列表。
    /// </summary>
    public List<SaveAiBusinessProviderRouteRequest> Routes { get; set; } = [];
}

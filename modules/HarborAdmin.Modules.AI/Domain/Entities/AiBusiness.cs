using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 业务配置。
/// </summary>
[Index("ux_ai_business_key", nameof(BusinessKey), true)]
public sealed class AiBusiness : AuditableEntity
{
    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 允许的调用方 Key，逗号分隔。
    /// </summary>
    public string? AllowedProducerKeys { get; set; }

    /// <summary>
    /// 服务间签名密钥引用。
    /// </summary>
    public string? SigningSecretRef { get; set; }

    /// <summary>
    /// 回调 Topic。
    /// </summary>
    public string? CallbackTopic { get; set; }

    /// <summary>
    /// 默认 Prompt Key。
    /// </summary>
    public string? PromptKey { get; set; }

    /// <summary>
    /// 绑定知识库 Key，逗号分隔。
    /// </summary>
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
    public int MaxContextTokens { get; set; }

    /// <summary>
    /// 上下文超限策略。
    /// </summary>
    public string ContextOverflowStrategy { get; set; } = "Reject";

    /// <summary>
    /// 失败策略。
    /// </summary>
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
    /// 默认输出格式。
    /// </summary>
    public string? OutputFormat { get; set; }

    /// <summary>
    /// 默认 JSON Schema。
    /// </summary>
    [Column(StringLength = -1)]
    public string? OutputJsonSchema { get; set; }

    /// <summary>
    /// 是否严格结构化输出。
    /// </summary>
    public bool OutputStrict { get; set; }

    /// <summary>
    /// 是否校验并重试。
    /// </summary>
    public bool OutputValidateAndRetry { get; set; }

    /// <summary>
    /// 最大输出校验重试次数。
    /// </summary>
    public int OutputMaxRetryCount { get; set; }

    /// <summary>
    /// 工具配置 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? ToolOptionsJson { get; set; }

    /// <summary>
    /// 最大工具调用轮次。
    /// </summary>
    public int MaxToolRounds { get; set; }

    /// <summary>
    /// 默认供应商选项 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? ProviderOptionsJson { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 供应商路由。
    /// </summary>
    [Navigate(nameof(AiBusinessProviderRoute.BusinessId))]
    public List<AiBusinessProviderRoute> Routes { get; set; } = [];
}

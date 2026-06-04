namespace HarborAdmin.Client.AI.Constants;

/// <summary>
/// AI 错误码。
/// </summary>
public static class AiErrorCodes
{
    /// <summary>
    /// 请求不合法。
    /// </summary>
    public const string InvalidRequest = "AI_INVALID_REQUEST";

    /// <summary>
    /// 内部签名无效。
    /// </summary>
    public const string InvalidSignature = "AI_INVALID_SIGNATURE";

    /// <summary>
    /// 调用方无权访问业务。
    /// </summary>
    public const string ProducerNotAllowed = "AI_PRODUCER_NOT_ALLOWED";

    /// <summary>
    /// 配额不足。
    /// </summary>
    public const string QuotaExceeded = "AI_QUOTA_EXCEEDED";

    /// <summary>
    /// 上下文超过限制。
    /// </summary>
    public const string ContextTooLarge = "AI_CONTEXT_TOO_LARGE";

    /// <summary>
    /// 运行时覆盖未被允许。
    /// </summary>
    public const string OverrideNotAllowed = "AI_OVERRIDE_NOT_ALLOWED";

    /// <summary>
    /// AI 模型未配置。
    /// </summary>
    public const string ModelNotConfigured = "AI_MODEL_NOT_CONFIGURED";

    /// <summary>
    /// 供应商不可用。
    /// </summary>
    public const string ProviderUnavailable = "AI_PROVIDER_UNAVAILABLE";

    /// <summary>
    /// 知识库覆盖未被允许。
    /// </summary>
    public const string KnowledgeOverrideNotAllowed = "AI_KNOWLEDGE_OVERRIDE_NOT_ALLOWED";

    /// <summary>
    /// 业务不存在。
    /// </summary>
    public const string BusinessNotFound = "AI_BUSINESS_NOT_FOUND";

    /// <summary>
    /// 流式调用未启用。
    /// </summary>
    public const string StreamingNotEnabled = "AI_STREAMING_NOT_ENABLED";
}


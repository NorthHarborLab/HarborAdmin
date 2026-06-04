namespace HarborAdmin.Client.AI.Options;

/// <summary>
/// AI 模块选项。
/// </summary>
public sealed class AiOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Harbor:AI";

    /// <summary>
    /// AIWorker 内部 HTTP 地址。
    /// </summary>
    public string WorkerBaseUrl { get; set; } = "http://localhost:9510";

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    public string ProducerKey { get; set; } = "harbor-admin";

    /// <summary>
    /// 签名密钥引用。
    /// </summary>
    public string? SigningSecretRef { get; set; }

    /// <summary>
    /// 签名密钥明文。仅用于本地开发或外部服务自行注入。
    /// </summary>
    public string? SigningSecret { get; set; }

    /// <summary>
    /// 默认请求超时秒数。
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 默认流式请求超时秒数。
    /// </summary>
    public int StreamingTimeoutSeconds { get; set; } = 300;
}

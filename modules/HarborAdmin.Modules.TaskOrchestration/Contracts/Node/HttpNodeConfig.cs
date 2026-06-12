namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Node;

/// <summary>
/// HTTP 节点配置
/// </summary>
internal sealed class HttpNodeConfig
{
    /// <summary>
    /// HTTP Method
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 请求 URL 模板
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 请求头模板
    /// </summary>
    public Dictionary<string, string?>? Headers { get; set; }

    /// <summary>
    /// 请求体模板
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// 请求体内容类型
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 请求超时秒数
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

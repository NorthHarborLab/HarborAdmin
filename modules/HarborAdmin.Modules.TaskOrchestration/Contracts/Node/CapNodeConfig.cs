namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Node;

/// <summary>
/// CAP 节点配置
/// </summary>
internal sealed class CapNodeConfig
{
    /// <summary>
    /// 执行模式
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// CAP Topic
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Payload JSON 模板
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// 请求响应等待超时秒数
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

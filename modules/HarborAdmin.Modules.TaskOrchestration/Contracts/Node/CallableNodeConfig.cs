namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Node;

/// <summary>
/// 接口调用节点配置
/// </summary>
internal sealed class CallableNodeConfig
{
    /// <summary>
    /// 服务键
    /// </summary>
    public string? ServiceKey { get; set; }

    /// <summary>
    /// Callable 实现完整类名
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// Callable 实现完整类名
    /// </summary>
    public string? FullClassName { get; set; }

    /// <summary>
    /// Callable 实现类型名
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// 方法键
    /// </summary>
    public string? MethodKey { get; set; }

    /// <summary>
    /// 请求 JSON 模板
    /// </summary>
    public string? RequestJson { get; set; }
}

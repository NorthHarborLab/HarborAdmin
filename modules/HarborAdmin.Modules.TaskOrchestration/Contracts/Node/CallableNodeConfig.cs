namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Node;

/// <summary>
/// 接口调用节点配置
/// </summary>
internal sealed class CallableNodeConfig
{
    /// <summary>
    /// Callable 实现完整类名
    /// </summary>
    public string? FullClassName { get; set; }

    /// <summary>
    /// 请求 JSON 模板
    /// </summary>
    public string? RequestJson { get; set; }
}

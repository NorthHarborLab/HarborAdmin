namespace HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

/// <summary>
/// 节点执行器类型
/// </summary>
public static class OrchestrationExecutorTypes
{
    /// <summary>
    /// 起始节点
    /// </summary>
    public const string Start = "start";

    /// <summary>
    /// 结束节点
    /// </summary>
    public const string End = "end";

    /// <summary>
    /// HTTP 节点
    /// </summary>
    public const string Http = "http";

    /// <summary>
    /// CAP 节点
    /// </summary>
    public const string Cap = "cap";

    /// <summary>
    /// 显式注册接口调用节点
    /// </summary>
    public const string Callable = "callable";
}

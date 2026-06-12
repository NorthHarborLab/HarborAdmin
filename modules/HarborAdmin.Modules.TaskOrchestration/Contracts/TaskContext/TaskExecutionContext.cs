using System.Text.Json.Nodes;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.TaskContext;

/// <summary>
/// 任务执行上下文
/// </summary>
public sealed class TaskExecutionContext
{
    /// <summary>
    /// 运行参数
    /// </summary>
    public JsonNode? Params { get; init; }

    /// <summary>
    /// 已完成节点输出索引
    /// </summary>
    public Dictionary<string, TaskNodeResult> Nodes { get; } = new(StringComparer.OrdinalIgnoreCase);
}

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

/// <summary>
/// 节点执行上下文
/// </summary>
/// <param name="TaskRunId">任务运行标识</param>
/// <param name="NodeCode">节点编码</param>
/// <param name="ExecutorType">执行器类型</param>
/// <param name="ConfigJson">节点配置 JSON</param>
/// <param name="TimeoutSeconds">节点超时秒数</param>
/// <param name="ExecutionContext">任务执行上下文</param>
public sealed record TaskNodeExecutionContext(
    long TaskRunId,
    string NodeCode,
    string ExecutorType,
    string? ConfigJson,
    int TimeoutSeconds,
    TaskExecutionContext ExecutionContext);

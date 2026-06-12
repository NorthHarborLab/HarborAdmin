using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

/// <summary>
/// 任务节点执行器
/// </summary>
public interface ITaskStepExecutor
{
    /// <summary>
    /// 执行器类型
    /// </summary>
    string ExecutorType { get; }

    /// <summary>
    /// 执行任务节点
    /// </summary>
    /// <param name="context">任务节点执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点输出</returns>
    Task<JsonNode?> ExecuteAsync(TaskNodeExecutionContext context, CancellationToken cancellationToken);
}

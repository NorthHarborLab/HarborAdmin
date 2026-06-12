using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Node;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Services;

/// <summary>
/// 编排任务节点执行服务
/// </summary>
public sealed class TaskNodeExecutionService(ITaskRunRepository runRepository, IEnumerable<ITaskStepExecutor> executors)
{
    private readonly IReadOnlyDictionary<string, ITaskStepExecutor> _executors =
        executors.ToDictionary(item => item.ExecutorType, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 执行单个 DAG 节点并持久化节点日志
    /// </summary>
    internal async Task<NodeExecutionResult> ExecuteAsync(OrchestrationTaskRun run, OrchestrationTaskNode node, TaskExecutionContext context,
        CancellationToken cancellationToken)
    {
        var nodeRun = CreateNodeRun(run, node, OrchestrationRunStatus.Running, null);
        nodeRun.StartedAt = DateTimeOffset.UtcNow;
        await runRepository.InsertNodeRunAsync(nodeRun, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var output = await ExecuteNodeCoreAsync(run, node, context, cancellationToken);
            nodeRun.OutputJson = output?.ToJsonString();
            nodeRun.Status = OrchestrationRunStatus.Succeeded;
            return new NodeExecutionResult(node.NodeCode, nodeRun.Status, output);
        }
        catch (Exception ex)
        {
            nodeRun.Status = OrchestrationRunStatus.Failed;
            nodeRun.ErrorMessage = ex.Message;
            return new NodeExecutionResult(node.NodeCode, nodeRun.Status, JsonSerializer.SerializeToNode(new { error = ex.Message }));
        }
        finally
        {
            stopwatch.Stop();
            nodeRun.DurationMilliseconds = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);
            nodeRun.FinishedAt = DateTimeOffset.UtcNow;
            await runRepository.UpdateNodeRunAsync(nodeRun, CancellationToken.None);
        }
    }

    /// <summary>
    /// 为未进入执行的节点补充跳过日志
    /// </summary>
    public async Task InsertSkippedAsync(OrchestrationTaskRun run, IEnumerable<OrchestrationTaskNode> nodes, IReadOnlySet<string> completed,
        IReadOnlySet<string> blocked, bool stopped, CancellationToken cancellationToken)
    {
        foreach (var node in nodes.Where(node => !completed.Contains(node.NodeCode)))
        {
            var reason = blocked.Contains(node.NodeCode)
                ? "上游节点失败，依赖分支被阻断"
                : stopped
                    ? "上游节点失败，任务配置为停止全部"
                    : "上游条件或汇聚策略未满足，节点未进入执行";
            await runRepository.InsertNodeRunAsync(CreateNodeRun(run, node, OrchestrationRunStatus.Skipped, reason), cancellationToken);
        }
    }

    /// <summary>
    /// 执行节点核心逻辑
    /// </summary>
    private async Task<JsonNode?> ExecuteNodeCoreAsync(OrchestrationTaskRun run, OrchestrationTaskNode node,
        TaskExecutionContext context, CancellationToken cancellationToken)
    {
        if (node.ExecutorType is OrchestrationExecutorTypes.Start or OrchestrationExecutorTypes.End)
        {
            return JsonSerializer.SerializeToNode(new { ok = true });
        }

        if (!_executors.TryGetValue(node.ExecutorType, out var executor))
        {
            throw new InvalidOperationException($"Executor '{node.ExecutorType}' is not registered.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(node.TimeoutSeconds, 1, 3600)));
        return await executor.ExecuteAsync(
            new TaskNodeExecutionContext(run.Id, node.NodeCode, node.ExecutorType, node.ConfigJson, node.TimeoutSeconds, context),
            timeout.Token);
    }

    /// <summary>
    /// 创建节点运行日志
    /// </summary>
    private static OrchestrationNodeRun CreateNodeRun(OrchestrationTaskRun run, OrchestrationTaskNode node, OrchestrationRunStatus status, string? errorMessage) =>
        new()
        {
            TaskRunId = run.Id,
            TaskId = run.TaskId,
            NodeCode = node.NodeCode,
            NodeName = node.Name,
            ExecutorType = node.ExecutorType,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            FinishedAt = status == OrchestrationRunStatus.Skipped ? DateTimeOffset.UtcNow : null,
            InputJson = node.ConfigJson,
            ErrorMessage = errorMessage,
        };
}
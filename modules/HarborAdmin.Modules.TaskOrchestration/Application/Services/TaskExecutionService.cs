using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Application.Execution;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Result;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Services;

/// <summary>
/// DAG 编排任务执行服务
/// </summary>
public sealed class TaskExecutionService(
    ITaskDefinitionRepository taskRepository,
    ITaskRunRepository runRepository,
    TaskNodeExecutionService nodeExecutionService,
    TaskConditionEvaluator conditionEvaluator,
    ILogger<TaskExecutionService> logger)
{
    /// <summary>
    /// 执行一次编排任务运行请求
    /// </summary>
    public async Task ExecuteAsync(TaskRunRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetTaskAsync(request.TaskId, cancellationToken);
        if (task is null || !task.Enabled)
        {
            return;
        }

        if (!task.AllowConcurrentRuns && await runRepository.HasRunningTaskAsync(task.Id, cancellationToken))
        {
            logger.LogInformation("Task {TaskCode} already has a running instance.", task.TaskCode);
            return;
        }

        var run = new OrchestrationTaskRun
        {
            TaskId = task.Id,
            TaskCode = task.TaskCode,
            TriggerType = request.TriggerType,
            TriggerCode = request.TriggerCode,
            CorrelationId = request.CorrelationId,
            ParamsJson = MergeParams(task.DefaultParamsJson, request.ParamsJson),
            TriggerPayloadJson = request.TriggerPayloadJson,
            CreatedAt = DateTimeOffset.UtcNow,
            StartedAt = DateTimeOffset.UtcNow,
            Status = OrchestrationRunStatus.Running,
        };
        await runRepository.InsertRunAsync(run, cancellationToken);

        try
        {
            var context = new TaskExecutionContext { Params = ParseJson(run.ParamsJson) };
            var nodes = task.Nodes.Where(node => node.Enabled).ToDictionary(node => node.NodeCode, StringComparer.OrdinalIgnoreCase);
            var incoming = task.Edges.Where(edge => edge.Enabled)
                .GroupBy(edge => edge.TargetNodeCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var outgoing = task.Edges.Where(edge => edge.Enabled)
                .GroupBy(edge => edge.SourceNodeCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stopAll = false;

            while (!stopAll)
            {
                var ready = nodes.Values
                    .Where(node => !completed.Contains(node.NodeCode) && !blocked.Contains(node.NodeCode))
                    .Where(node => IsReady(node, incoming, context, completed, blocked))
                    .ToArray();
                if (ready.Length == 0)
                {
                    break;
                }

                var results = await Task.WhenAll(ready.Select(node => nodeExecutionService.ExecuteAsync(run, node, context, cancellationToken)));
                foreach (var result in results)
                {
                    completed.Add(result.NodeCode);
                    context.Nodes[result.NodeCode] = new TaskNodeResult(result.Status.ToString(), result.Output);
                    if (result.Status == OrchestrationRunStatus.Failed)
                    {
                        var node = nodes[result.NodeCode];
                        if (node.FailurePolicy == OrchestrationFailurePolicy.StopAll)
                        {
                            stopAll = true;
                        }
                        else if (node.FailurePolicy == OrchestrationFailurePolicy.BlockDependents)
                        {
                            BlockDependents(result.NodeCode, outgoing, blocked);
                        }
                    }
                }
            }

            await nodeExecutionService.InsertSkippedAsync(run, nodes.Values, completed, blocked, stopAll, cancellationToken);

            var failed = context.Nodes.Values.Any(item => string.Equals(item.Status, nameof(OrchestrationRunStatus.Failed), StringComparison.OrdinalIgnoreCase));
            var skipped = nodes.Count != completed.Count;
            run.Status = failed && skipped ? OrchestrationRunStatus.PartialFailed :
                failed ? OrchestrationRunStatus.Failed :
                OrchestrationRunStatus.Succeeded;
        }
        catch (Exception ex)
        {
            run.Status = OrchestrationRunStatus.Failed;
            run.ErrorMessage = ex.Message;
            logger.LogError(ex, "Task {TaskCode} run {RunId} failed.", task.TaskCode, run.Id);
        }
        finally
        {
            run.FinishedAt = DateTimeOffset.UtcNow;
            await runRepository.UpdateRunAsync(run, cancellationToken);
            await runRepository.UpdateTaskRunCountersAsync(run.TaskId, run.Status, run.StartedAt, cancellationToken);
        }
    }

    /// <summary>
    /// 判断节点上游依赖与条件是否满足
    /// </summary>
    private bool IsReady(OrchestrationTaskNode node, IReadOnlyDictionary<string, List<OrchestrationTaskEdge>> incoming,
        TaskExecutionContext context, IReadOnlySet<string> completed, IReadOnlySet<string> blocked)
    {
        if (!incoming.TryGetValue(node.NodeCode, out var edges) || edges.Count == 0)
        {
            return true;
        }

        var usable = edges.Where(edge => !blocked.Contains(edge.SourceNodeCode)).ToArray();
        if (usable.Length == 0)
        {
            return false;
        }

        return usable[0].JoinPolicy switch
        {
            OrchestrationJoinPolicy.AnySucceeded => usable.Any(edge =>
                completed.Contains(edge.SourceNodeCode)
                && context.Nodes.TryGetValue(edge.SourceNodeCode, out var result)
                && string.Equals(result.Status, nameof(OrchestrationRunStatus.Succeeded), StringComparison.OrdinalIgnoreCase)
                && conditionEvaluator.Evaluate(edge.ConditionExpression, context)),
            OrchestrationJoinPolicy.AllCompleted => usable.All(edge => completed.Contains(edge.SourceNodeCode))
                                                    && usable.All(edge => conditionEvaluator.Evaluate(edge.ConditionExpression, context)),
            _ => usable.All(edge =>
                completed.Contains(edge.SourceNodeCode)
                && context.Nodes.TryGetValue(edge.SourceNodeCode, out var result)
                && string.Equals(result.Status, nameof(OrchestrationRunStatus.Succeeded), StringComparison.OrdinalIgnoreCase)
                && conditionEvaluator.Evaluate(edge.ConditionExpression, context)),
        };
    }

    /// <summary>
    /// 标记失败节点的下游依赖分支为阻塞
    /// </summary>
    private static void BlockDependents(string nodeCode, IReadOnlyDictionary<string, List<OrchestrationTaskEdge>> outgoing, ISet<string> blocked)
    {
        if (!outgoing.TryGetValue(nodeCode, out var edges))
        {
            return;
        }

        foreach (var edge in edges)
        {
            if (blocked.Add(edge.TargetNodeCode))
            {
                BlockDependents(edge.TargetNodeCode, outgoing, blocked);
            }
        }
    }

    /// <summary>
    /// 解析 JSON 文本为空对象
    /// </summary>
    private static JsonNode? ParseJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new JsonObject() : JsonNode.Parse(json);

    /// <summary>
    /// 合并任务默认参数与运行时参数
    /// </summary>
    private static string MergeParams(string? defaults, string? runtime)
    {
        var merged = new JsonObject();
        MergeInto(merged, defaults);
        MergeInto(merged, runtime);
        return merged.ToJsonString();
    }

    /// <summary>
    /// 将 JSON 对象合并到目标对象
    /// </summary>
    private static void MergeInto(JsonObject target, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        if (JsonNode.Parse(json) is not JsonObject source)
        {
            return;
        }

        foreach (var item in source)
        {
            target[item.Key] = item.Value?.DeepClone();
        }
    }
}
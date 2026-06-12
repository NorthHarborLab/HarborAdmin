using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Services;

/// <summary>
/// 编排任务定义工厂
/// </summary>
internal static class TaskDefinitionFactory
{
    /// <summary>
    /// 将保存请求应用到任务聚合
    /// </summary>
    public static void Apply(OrchestrationTask task, SaveOrchestrationTaskRequest request, DateTimeOffset now)
    {
        task.TaskCode = Normalize(request.TaskCode, "任务编码");
        task.Name = Normalize(request.Name, "任务名称");
        task.Description = request.Description?.Trim();
        task.Enabled = request.Enabled;
        task.AllowConcurrentRuns = request.AllowConcurrentRuns;
        task.DefaultParamsJson = NormalizeJson(request.DefaultParamsJson);
        task.ParamSchemaJson = NormalizeJson(request.ParamSchemaJson);
        task.UpdatedAt = now;
        task.Triggers = request.Triggers.Select(item => CreateTrigger(task.Id, item, now)).ToList();
        task.Nodes = request.Nodes.Select(item => CreateNode(task.Id, item, now)).ToList();
        task.Edges = request.Edges.Select(item => CreateEdge(task.Id, item, now)).ToList();
    }

    /// <summary>
    /// 创建触发器实体
    /// </summary>
    private static OrchestrationTaskTrigger CreateTrigger(long taskId, SaveOrchestrationTaskTriggerRequest request, DateTimeOffset now) =>
        new()
        {
            TaskId = taskId,
            TriggerCode = Normalize(request.TriggerCode, "触发器编码"),
            TriggerType = request.TriggerType,
            CronExpression = request.TriggerType == OrchestrationTriggerType.Cron ? Normalize(request.CronExpression ?? string.Empty, "Cron 表达式") : null,
            TimeZoneId = request.TriggerType == OrchestrationTriggerType.Cron ? (request.TimeZoneId?.Trim() ?? "Asia/Shanghai") : null,
            TriggerTopic = request.TriggerType == OrchestrationTriggerType.Cap ? Normalize(request.TriggerTopic ?? string.Empty, "CAP Topic") : null,
            Enabled = request.Enabled,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>
    /// 创建任务节点实体
    /// </summary>
    private static OrchestrationTaskNode CreateNode(long taskId, SaveOrchestrationTaskNodeRequest request, DateTimeOffset now) =>
        new()
        {
            TaskId = taskId,
            NodeCode = Normalize(request.NodeCode, "节点编码"),
            Name = Normalize(request.Name, "节点名称"),
            ExecutorType = Normalize(request.ExecutorType, "执行器类型"),
            ConfigJson = NormalizeJson(request.ConfigJson),
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            TimeoutSeconds = request.TimeoutSeconds <= 0 ? 30 : request.TimeoutSeconds,
            RetryCount = Math.Max(0, request.RetryCount),
            FailurePolicy = request.FailurePolicy,
            Enabled = request.Enabled,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>
    /// 创建任务连线实体
    /// </summary>
    private static OrchestrationTaskEdge CreateEdge(long taskId, SaveOrchestrationTaskEdgeRequest request, DateTimeOffset now) =>
        new()
        {
            TaskId = taskId,
            EdgeCode = string.IsNullOrWhiteSpace(request.EdgeCode) ? $"{request.SourceNodeCode}-{request.TargetNodeCode}" : request.EdgeCode.Trim(),
            SourceNodeCode = Normalize(request.SourceNodeCode, "上游节点"),
            TargetNodeCode = Normalize(request.TargetNodeCode, "下游节点"),
            ConditionExpression = request.ConditionExpression?.Trim(),
            JoinPolicy = request.JoinPolicy,
            Enabled = request.Enabled,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>
    /// 规范化必填字符串
    /// </summary>
    private static string Normalize(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationDomainException($"{name}不能为空");
        }

        return value.Trim();
    }

    /// <summary>
    /// 规范化并校验 JSON 文本
    /// </summary>
    private static string? NormalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        _ = JsonDocument.Parse(json);
        return json.Trim();
    }
}

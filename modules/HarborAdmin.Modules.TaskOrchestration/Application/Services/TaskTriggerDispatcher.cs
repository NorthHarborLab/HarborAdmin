using System.Text.Json.Nodes;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Application.Events;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Services;

/// <summary>
/// 任务触发分发服务
/// </summary>
public sealed class TaskTriggerDispatcher(ITaskDefinitionRepository repository, IEventPublisher publisher)
{
    /// <summary>
    /// 分发手动运行请求
    /// </summary>
    /// <param name="task">编排任务</param>
    /// <param name="request">手动运行请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DispatchManualAsync(OrchestrationTask task, RunOrchestrationTaskRequest request, CancellationToken cancellationToken) =>
        PublishRunRequestAsync(new TaskRunRequest(task.Id, "manual", null, request.ParamsJson, null, request.CorrelationId), cancellationToken);

    /// <summary>
    /// 分发 Cron 运行请求
    /// </summary>
    /// <param name="taskId">任务标识</param>
    /// <param name="triggerCode">触发器编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DispatchCronAsync(long taskId, string triggerCode, CancellationToken cancellationToken) =>
        PublishRunRequestAsync(new TaskRunRequest(taskId, "cron", triggerCode, null, null, null), cancellationToken);

    /// <summary>
    /// 分发 CAP 触发运行请求
    /// </summary>
    /// <param name="topic">触发 Topic</param>
    /// <param name="request">CAP 触发请求</param>
    /// <param name="payload">原始触发消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DispatchCapAsync(string? topic, CapTaskTriggerRequest request, JsonNode? payload, CancellationToken cancellationToken)
    {
        IReadOnlyList<OrchestrationTask> tasks = [];
        if (!string.IsNullOrWhiteSpace(request.TaskCode))
        {
            var task = await repository.GetTaskByCodeAsync(request.TaskCode, cancellationToken);
            if (task is not null && task.Enabled)
            {
                tasks = [task];
            }
        }
        else if (!string.IsNullOrWhiteSpace(topic))
        {
            tasks = await repository.ListEnabledTasksByCapTopicAsync(topic, cancellationToken);
        }

        foreach (var task in tasks)
        {
            var trigger = task.Triggers.FirstOrDefault(item =>
                item is { Enabled: true, TriggerType: OrchestrationTriggerType.Cap }
                && (string.IsNullOrWhiteSpace(topic) || string.Equals(item.TriggerTopic, topic, StringComparison.OrdinalIgnoreCase)));
            if (trigger is null)
            {
                continue;
            }

            await PublishRunRequestAsync(new TaskRunRequest(
                task.Id,
                "cap",
                trigger.TriggerCode,
                request.ParamsJson,
                payload?.ToJsonString(),
                request.CorrelationId), cancellationToken);
        }
    }

    /// <summary>
    /// 发布任务运行请求
    /// </summary>
    /// <param name="request">任务运行请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    private Task PublishRunRequestAsync(TaskRunRequest request, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            TaskOrchestrationEventTopics.RunRequested,
            new TaskRunRequestedIntegrationEvent(
                request.TaskId,
                request.TriggerType,
                request.TriggerCode,
                request.ParamsJson,
                request.TriggerPayloadJson,
                request.CorrelationId),
            cancellationToken);
}

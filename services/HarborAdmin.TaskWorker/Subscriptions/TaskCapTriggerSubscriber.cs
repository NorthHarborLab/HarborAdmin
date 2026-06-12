using System.Text.Json.Nodes;
using System.Text.Json;
using DotNetCore.CAP;
using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

namespace HarborAdmin.TaskWorker.Subscriptions;

/// <summary>
/// CAP 任务触发订阅器。
/// </summary>
public sealed class TaskCapTriggerSubscriber(TaskTriggerDispatcher dispatcher) : ICapSubscribe
{
    public const string TriggerTopicPattern = "harbor.task.trigger.#";

    /// <summary>
    /// 接收 CAP 触发请求。
    /// </summary>
    [CapSubscribe(TriggerTopicPattern)]
    public async Task HandleAsync(JsonNode? payload, CancellationToken cancellationToken = default)
    {
        var request = payload is null
            ? new CapTaskTriggerRequest(null, null, null, null)
            : JsonSerializer.Deserialize<CapTaskTriggerRequest>(payload.ToJsonString()) ?? new CapTaskTriggerRequest(null, null, null, null);
        await dispatcher.DispatchCapAsync(null, request, payload, cancellationToken);
    }
}

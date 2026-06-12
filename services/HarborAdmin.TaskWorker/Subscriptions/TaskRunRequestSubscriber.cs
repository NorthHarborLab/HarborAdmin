using DotNetCore.CAP;
using HarborAdmin.Modules.TaskOrchestration.Application.Events;
using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

namespace HarborAdmin.TaskWorker.Subscriptions;

/// <summary>
/// 任务运行请求订阅器。
/// </summary>
public sealed class TaskRunRequestSubscriber(TaskExecutionService executionService) : ICapSubscribe
{
    /// <summary>
    /// 接收任务运行请求并执行 DAG。
    /// </summary>
    /// <param name="message">任务运行请求事件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [CapSubscribe(TaskOrchestrationEventTopics.RunRequested)]
    public Task HandleAsync(TaskRunRequestedIntegrationEvent message, CancellationToken cancellationToken = default) =>
        executionService.ExecuteAsync(
            new TaskRunRequest(
                message.TaskId,
                message.TriggerType,
                message.TriggerCode,
                message.ParamsJson,
                message.TriggerPayloadJson,
                message.CorrelationId),
            cancellationToken);
}

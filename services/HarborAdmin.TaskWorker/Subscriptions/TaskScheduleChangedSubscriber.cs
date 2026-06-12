using DotNetCore.CAP;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.TaskWorker.Scheduling;
using HarborAdmin.Modules.TaskOrchestration.Application.Events;
using Quartz;

namespace HarborAdmin.TaskWorker.Subscriptions;

/// <summary>
/// 任务调度变更订阅器
/// </summary>
public sealed class TaskScheduleChangedSubscriber(ITaskDefinitionRepository repository, ISchedulerFactory schedulerFactory) : ICapSubscribe
{
    /// <summary>
    /// 接收任务调度变更并同步 Quartz
    /// </summary>
    /// <param name="message">任务调度变更事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    [CapSubscribe(TaskOrchestrationEventTopics.ScheduleChanged)]
    public async Task HandleAsync(TaskScheduleChangedIntegrationEvent message, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        if (message.Deleted)
        {
            await TaskQuartzScheduleHostedService.UnscheduleTaskAsync(scheduler, message.TaskId, cancellationToken);
            return;
        }

        var task = await repository.GetTaskAsync(message.TaskId, cancellationToken);
        if (task is null)
        {
            await TaskQuartzScheduleHostedService.UnscheduleTaskAsync(scheduler, message.TaskId, cancellationToken);
            return;
        }

        await TaskQuartzScheduleHostedService.SyncTaskAsync(scheduler, task, cancellationToken);
    }
}

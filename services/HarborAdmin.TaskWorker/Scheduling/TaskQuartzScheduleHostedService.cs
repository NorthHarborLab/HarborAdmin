using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;
using Quartz;
using Quartz.Impl.Matchers;

namespace HarborAdmin.TaskWorker.Scheduling;

/// <summary>
/// 启动时同步 Quartz 触发器。
/// </summary>
public sealed class TaskQuartzScheduleHostedService(IServiceScopeFactory scopeFactory, ISchedulerFactory schedulerFactory) : IHostedService
{
    /// <summary>
    /// 启动时同步全部已启用任务的 Cron 触发器。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITaskDefinitionRepository>();
        var tasks = await repository.ListEnabledTasksAsync(cancellationToken);
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        foreach (var task in tasks)
        {
            await SyncTaskAsync(scheduler, task, cancellationToken);
        }
    }

    /// <summary>
    /// 停止调度同步服务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 同步指定任务的 Quartz 触发器。
    /// </summary>
    /// <param name="scheduler">Quartz 调度器。</param>
    /// <param name="task">编排任务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task SyncTaskAsync(IScheduler scheduler, OrchestrationTask task, CancellationToken cancellationToken)
    {
        await UnscheduleTaskAsync(scheduler, task.Id, cancellationToken);
        foreach (var trigger in task.Triggers.Where(item => item.TriggerType == OrchestrationTriggerType.Cron))
        {
            var triggerKey = GetTriggerKey(task.Id, trigger.TriggerCode);
            if (!task.Enabled || !trigger.Enabled || string.IsNullOrWhiteSpace(trigger.CronExpression))
            {
                continue;
            }

            var schedule = CronScheduleBuilder.CronSchedule(trigger.CronExpression);
            if (!string.IsNullOrWhiteSpace(trigger.TimeZoneId))
            {
                schedule = schedule.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(trigger.TimeZoneId));
            }

            var quartzTrigger = TriggerBuilder.Create()
                .ForJob(TaskQuartzJob.JobKey)
                .WithIdentity(triggerKey)
                .UsingJobData("taskId", task.Id)
                .UsingJobData("triggerCode", trigger.TriggerCode)
                .WithSchedule(schedule)
                .Build();
            await scheduler.ScheduleJob(quartzTrigger, cancellationToken);
        }
    }

    /// <summary>
    /// 移除指定任务的全部 Quartz 触发器。
    /// </summary>
    /// <param name="scheduler">Quartz 调度器。</param>
    /// <param name="taskId">任务标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task UnscheduleTaskAsync(IScheduler scheduler, long taskId, CancellationToken cancellationToken)
    {
        var prefix = $"task-{taskId}-";
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals("task-orchestration"), cancellationToken);
        foreach (var triggerKey in triggerKeys.Where(item => item.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await scheduler.UnscheduleJob(triggerKey, cancellationToken);
        }
    }

    /// <summary>
    /// 获取 Quartz 触发器键。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="triggerCode">触发器编码。</param>
    /// <returns>Quartz 触发器键。</returns>
    public static TriggerKey GetTriggerKey(long taskId, string triggerCode) =>
        new($"task-{taskId}-{triggerCode}", "task-orchestration");
}

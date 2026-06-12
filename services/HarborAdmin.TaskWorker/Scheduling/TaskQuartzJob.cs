using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using Quartz;

namespace HarborAdmin.TaskWorker.Scheduling;

/// <summary>
/// Quartz 编排任务触发 Job。
/// </summary>
public sealed class TaskQuartzJob(TaskTriggerDispatcher dispatcher) : IJob
{
    /// <summary>
    /// Quartz Job 键。
    /// </summary>
    public static readonly JobKey JobKey = new("task-orchestration-dispatcher", "task-orchestration");

    /// <summary>
    /// 执行 Quartz 触发回调。
    /// </summary>
    /// <param name="context">Quartz Job 上下文。</param>
    public async Task Execute(IJobExecutionContext context)
    {
        var taskId = context.MergedJobDataMap.GetLong("taskId");
        var triggerCode = context.MergedJobDataMap.GetString("triggerCode") ?? string.Empty;
        await dispatcher.DispatchCronAsync(taskId, triggerCode, context.CancellationToken);
    }
}

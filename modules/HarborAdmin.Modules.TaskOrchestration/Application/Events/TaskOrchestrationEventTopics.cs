namespace HarborAdmin.Modules.TaskOrchestration.Application.Events;

/// <summary>
/// 任务编排内部事件 Topic
/// </summary>
public static class TaskOrchestrationEventTopics
{
    /// <summary>
    /// 任务运行请求 Topic
    /// </summary>
    public const string RunRequested = "harbor.task.run.request.v1";

    /// <summary>
    /// 任务调度变更 Topic
    /// </summary>
    public const string ScheduleChanged = "harbor.task.schedule.changed.v1";
}

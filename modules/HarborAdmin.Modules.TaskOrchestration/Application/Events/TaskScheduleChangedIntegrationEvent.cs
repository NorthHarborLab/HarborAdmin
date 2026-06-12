namespace HarborAdmin.Modules.TaskOrchestration.Application.Events;

/// <summary>
/// 任务调度变更集成事件
/// </summary>
/// <param name="TaskId">任务标识</param>
/// <param name="Deleted">是否已删除</param>
public sealed record TaskScheduleChangedIntegrationEvent(long TaskId, bool Deleted);

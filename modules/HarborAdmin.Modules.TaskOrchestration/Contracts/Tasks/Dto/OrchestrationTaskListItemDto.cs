namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// 编排任务列表项 DTO
/// </summary>
/// <param name="Id">任务 ID</param>
/// <param name="TaskCode">任务编码</param>
/// <param name="Name">任务名称</param>
/// <param name="Enabled">是否启用</param>
/// <param name="AllowConcurrentRuns">是否允许并发运行</param>
/// <param name="TriggerCount">触发器数量</param>
/// <param name="NodeCount">节点数量</param>
/// <param name="TotalRunCount">总执行次数</param>
/// <param name="SucceededRunCount">成功执行次数</param>
/// <param name="FailedRunCount">失败执行次数</param>
/// <param name="InterruptedRunCount">中断执行次数</param>
/// <param name="LastStartedAt">上次开始执行时间</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="UpdatedAt">更新时间</param>
public sealed record OrchestrationTaskListItemDto(
    long Id,
    string TaskCode,
    string Name,
    bool Enabled,
    bool AllowConcurrentRuns,
    int TriggerCount,
    int NodeCount,
    long TotalRunCount,
    long SucceededRunCount,
    long FailedRunCount,
    long InterruptedRunCount,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

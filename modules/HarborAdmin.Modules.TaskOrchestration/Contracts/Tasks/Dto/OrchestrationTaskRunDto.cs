using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// 任务运行 DTO
/// </summary>
/// <param name="Id">运行记录 ID</param>
/// <param name="TaskId">任务 ID</param>
/// <param name="TaskCode">任务编码</param>
/// <param name="TriggerType">触发类型</param>
/// <param name="TriggerCode">触发器编码</param>
/// <param name="CorrelationId">关联标识</param>
/// <param name="ParamsJson">运行参数 JSON</param>
/// <param name="TriggerPayloadJson">触发消息 JSON</param>
/// <param name="Status">运行状态</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="StartedAt">开始时间</param>
/// <param name="FinishedAt">完成时间</param>
/// <param name="ErrorMessage">错误信息</param>
/// <param name="Nodes">节点运行日志集合</param>
public sealed record OrchestrationTaskRunDto(
    long Id,
    long TaskId,
    string TaskCode,
    string TriggerType,
    string? TriggerCode,
    string? CorrelationId,
    string? ParamsJson,
    string? TriggerPayloadJson,
    OrchestrationRunStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ErrorMessage,
    IReadOnlyList<OrchestrationNodeRunDto> Nodes);

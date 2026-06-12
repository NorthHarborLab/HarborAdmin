using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// 触发器 DTO
/// </summary>
/// <param name="Id">触发器 ID</param>
/// <param name="TriggerCode">触发器编码</param>
/// <param name="TriggerType">触发器类型</param>
/// <param name="CronExpression">Cron 表达式</param>
/// <param name="TimeZoneId">Cron 时区 ID</param>
/// <param name="TriggerTopic">CAP 触发 Topic</param>
/// <param name="Enabled">是否启用</param>
public sealed record OrchestrationTaskTriggerDto(
    long Id,
    string TriggerCode,
    OrchestrationTriggerType TriggerType,
    string? CronExpression,
    string? TimeZoneId,
    string? TriggerTopic,
    bool Enabled);

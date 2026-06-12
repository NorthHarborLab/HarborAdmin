using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

/// <summary>
/// 编排任务触发器
/// </summary>
[Index("idx_orchestration_trigger_task", nameof(TaskId), false)]
[Index("idx_orchestration_trigger_cap_topic", nameof(TriggerTopic), false)]
public sealed class OrchestrationTaskTrigger : AuditableEntity
{
    /// <summary>
    /// 所属任务 ID
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 所属任务
    /// </summary>
    [Navigate(nameof(TaskId))]
    public OrchestrationTask? Task { get; set; }

    /// <summary>
    /// 触发器编码
    /// </summary>
    public string TriggerCode { get; set; } = string.Empty;

    /// <summary>
    /// 触发器类型
    /// </summary>
    public OrchestrationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Cron 表达式
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Cron 时区 ID
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// CAP 触发 Topic
    /// </summary>
    public string? TriggerTopic { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}

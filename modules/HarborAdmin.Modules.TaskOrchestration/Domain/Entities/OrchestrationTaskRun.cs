using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

/// <summary>
/// 编排任务运行记录
/// </summary>
[Index("idx_orchestration_run_task", nameof(TaskId), false)]
public sealed class OrchestrationTaskRun : EntityBase
{
    /// <summary>
    /// 任务 ID
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 任务编码
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 触发类型
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// 触发器编码
    /// </summary>
    public string? TriggerCode { get; set; }

    /// <summary>
    /// 关联标识
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// 运行参数 JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? ParamsJson { get; set; }

    /// <summary>
    /// 触发消息 JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? TriggerPayloadJson { get; set; }

    /// <summary>
    /// 运行状态
    /// </summary>
    public OrchestrationRunStatus Status { get; set; } = OrchestrationRunStatus.Pending;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [Column(StringLength = -1)]
    public string? ErrorMessage { get; set; }
}

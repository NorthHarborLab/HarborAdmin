using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

/// <summary>
/// 编排节点运行日志
/// </summary>
[Index("idx_orchestration_node_run_task_run", nameof(TaskRunId), false)]
public sealed class OrchestrationNodeRun : EntityBase
{
    /// <summary>
    /// 任务运行 ID
    /// </summary>
    public long TaskRunId { get; set; }

    /// <summary>
    /// 任务 ID
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 节点编码
    /// </summary>
    public string NodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 执行器类型
    /// </summary>
    public string ExecutorType { get; set; } = string.Empty;

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
    /// 执行耗时毫秒数
    /// </summary>
    public int DurationMilliseconds { get; set; }

    /// <summary>
    /// 节点输入 JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? InputJson { get; set; }

    /// <summary>
    /// 节点输出 JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? OutputJson { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [Column(StringLength = -1)]
    public string? ErrorMessage { get; set; }
}

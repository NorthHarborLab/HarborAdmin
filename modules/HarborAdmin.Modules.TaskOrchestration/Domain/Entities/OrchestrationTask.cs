using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

/// <summary>
/// DAG 编排任务
/// </summary>
[Index("ux_orchestration_task_code", nameof(TaskCode), true)]
public sealed class OrchestrationTask : AuditableEntity
{
    /// <summary>
    /// 任务编码
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务说明
    /// </summary>
    [Column(StringLength = -1)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否允许并发运行
    /// </summary>
    public bool AllowConcurrentRuns { get; set; }

    /// <summary>
    /// 默认运行参数 JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? DefaultParamsJson { get; set; }

    /// <summary>
    /// 参数 Schema JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? ParamSchemaJson { get; set; }

    /// <summary>
    /// 总执行次数
    /// </summary>
    public long TotalRunCount { get; set; }

    /// <summary>
    /// 成功执行次数
    /// </summary>
    public long SucceededRunCount { get; set; }

    /// <summary>
    /// 失败执行次数
    /// </summary>
    public long FailedRunCount { get; set; }

    /// <summary>
    /// 中断执行次数
    /// </summary>
    public long InterruptedRunCount { get; set; }

    /// <summary>
    /// 上次开始执行时间
    /// </summary>
    [Column(IsNullable = true)]
    public DateTimeOffset? LastStartedAt { get; set; }

    /// <summary>
    /// 触发器
    /// </summary>
    [Navigate(nameof(OrchestrationTaskTrigger.TaskId))]
    public List<OrchestrationTaskTrigger> Triggers { get; set; } = [];

    /// <summary>
    /// DAG 节点
    /// </summary>
    [Navigate(nameof(OrchestrationTaskNode.TaskId))]
    public List<OrchestrationTaskNode> Nodes { get; set; } = [];

    /// <summary>
    /// DAG 连线
    /// </summary>
    [Navigate(nameof(OrchestrationTaskEdge.TaskId))]
    public List<OrchestrationTaskEdge> Edges { get; set; } = [];
}

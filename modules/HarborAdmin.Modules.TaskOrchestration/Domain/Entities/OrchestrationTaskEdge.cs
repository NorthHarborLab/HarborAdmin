using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

/// <summary>
/// 编排任务 DAG 连线
/// </summary>
[Index("idx_orchestration_edge_task", nameof(TaskId), false)]
public sealed class OrchestrationTaskEdge : AuditableEntity
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
    /// 连线编码
    /// </summary>
    public string EdgeCode { get; set; } = string.Empty;

    /// <summary>
    /// 上游节点编码
    /// </summary>
    public string SourceNodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 下游节点编码
    /// </summary>
    public string TargetNodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 条件表达式
    /// </summary>
    [Column(StringLength = -1)]
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// 汇聚策略
    /// </summary>
    public OrchestrationJoinPolicy JoinPolicy { get; set; } = OrchestrationJoinPolicy.AllSucceeded;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}

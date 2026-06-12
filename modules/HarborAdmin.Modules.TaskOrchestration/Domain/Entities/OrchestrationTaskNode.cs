using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

/// <summary>
/// 编排任务 DAG 节点
/// </summary>
[Index("ux_orchestration_node_task_code", $"{nameof(TaskId)},{nameof(NodeCode)}", true)]
public sealed class OrchestrationTaskNode : AuditableEntity
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
    /// 节点编码
    /// </summary>
    public string NodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 执行器类型
    /// </summary>
    public string ExecutorType { get; set; } = string.Empty;

    /// <summary>
    /// 节点配置 JSON
    /// </summary>
    [Column(StringLength = -1)]
    public string? ConfigJson { get; set; }

    /// <summary>
    /// 画布 X 坐标
    /// </summary>
    public int PositionX { get; set; }

    /// <summary>
    /// 画布 Y 坐标
    /// </summary>
    public int PositionY { get; set; }

    /// <summary>
    /// 超时秒数
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 失败策略
    /// </summary>
    public OrchestrationFailurePolicy FailurePolicy { get; set; } = OrchestrationFailurePolicy.BlockDependents;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 保存编排任务连线请求
/// </summary>
public sealed class SaveOrchestrationTaskEdgeRequest
{
    /// <summary>
    /// 连线编码
    /// </summary>
    [MaxLength(128, ErrorMessage = "连线编码不能超过 128 个字符")]
    public string EdgeCode { get; set; } = string.Empty;

    /// <summary>
    /// 上游节点编码
    /// </summary>
    [Required(ErrorMessage = "上游节点编码不能为空")]
    [MaxLength(64, ErrorMessage = "上游节点编码不能超过 64 个字符")]
    public string SourceNodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 下游节点编码
    /// </summary>
    [Required(ErrorMessage = "下游节点编码不能为空")]
    [MaxLength(64, ErrorMessage = "下游节点编码不能超过 64 个字符")]
    public string TargetNodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 条件表达式
    /// </summary>
    [MaxLength(512, ErrorMessage = "条件表达式不能超过 512 个字符")]
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

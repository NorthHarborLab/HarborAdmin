using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 保存编排任务节点请求
/// </summary>
public sealed class SaveOrchestrationTaskNodeRequest
{
    /// <summary>
    /// 节点编码
    /// </summary>
    [Required(ErrorMessage = "节点编码不能为空")]
    [MaxLength(64, ErrorMessage = "节点编码不能超过 64 个字符")]
    public string NodeCode { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    [Required(ErrorMessage = "节点名称不能为空")]
    [MaxLength(128, ErrorMessage = "节点名称不能超过 128 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 执行器类型
    /// </summary>
    [Required(ErrorMessage = "执行器类型不能为空")]
    [MaxLength(64, ErrorMessage = "执行器类型不能超过 64 个字符")]
    public string ExecutorType { get; set; } = string.Empty;

    /// <summary>
    /// 节点配置 JSON
    /// </summary>
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
    [Range(1, 3600, ErrorMessage = "超时秒数必须在 1 到 3600 之间")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 重试次数
    /// </summary>
    [Range(0, 20, ErrorMessage = "重试次数必须在 0 到 20 之间")]
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

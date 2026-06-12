using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 编排任务运行日志查询请求
/// </summary>
public sealed class QueryOrchestrationTaskRunRequest : PageRequest
{
    /// <summary>
    /// 任务 ID
    /// </summary>
    public long? TaskId { get; set; }

    /// <summary>
    /// 任务编码
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 运行状态
    /// </summary>
    public OrchestrationRunStatus? Status { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    public string? TriggerType { get; set; }

    /// <summary>
    /// 关联 ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// 创建时间起始
    /// </summary>
    public DateTimeOffset? CreatedFrom { get; set; }

    /// <summary>
    /// 创建时间结束
    /// </summary>
    public DateTimeOffset? CreatedTo { get; set; }
}

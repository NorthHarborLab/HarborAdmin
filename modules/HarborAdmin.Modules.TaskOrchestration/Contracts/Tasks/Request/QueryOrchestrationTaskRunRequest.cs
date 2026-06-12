using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

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
}

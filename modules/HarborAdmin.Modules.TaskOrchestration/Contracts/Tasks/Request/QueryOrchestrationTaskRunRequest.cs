using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 编排任务运行日志查询请求
/// </summary>
public sealed class QueryOrchestrationTaskRunRequest : HarborQueryOptions
{
    /// <summary>
    /// 任务 ID
    /// </summary>
    public long? TaskId { get; set; }
}

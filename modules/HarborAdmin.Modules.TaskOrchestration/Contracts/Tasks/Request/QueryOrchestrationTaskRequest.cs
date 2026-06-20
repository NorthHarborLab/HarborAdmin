using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 编排任务查询请求
/// </summary>
public sealed class QueryOrchestrationTaskRequest : HarborQueryOptions
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 启用状态
    /// </summary>
    public bool? Enabled { get; set; }
}

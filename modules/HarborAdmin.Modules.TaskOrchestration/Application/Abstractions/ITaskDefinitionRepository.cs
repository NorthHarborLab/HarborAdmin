using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

/// <summary>
/// 编排任务定义仓储
/// </summary>
public interface ITaskDefinitionRepository
{
    /// <summary>
    /// 分页查询编排任务
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务分页结果</returns>
    Task<PagedResult<OrchestrationTask>> QueryTasksAsync(QueryOrchestrationTaskRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 按 ID 获取任务聚合
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务聚合</returns>
    Task<OrchestrationTask?> GetTaskAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 按任务编码获取任务聚合
    /// </summary>
    /// <param name="taskCode">任务编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务聚合</returns>
    Task<OrchestrationTask?> GetTaskByCodeAsync(string taskCode, CancellationToken cancellationToken);

    /// <summary>
    /// 列出全部已启用任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用任务集合</returns>
    Task<IReadOnlyList<OrchestrationTask>> ListEnabledTasksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 按 CAP Topic 列出已启用任务
    /// </summary>
    /// <param name="triggerTopic">CAP 触发 Topic</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配 Topic 的已启用任务集合</returns>
    Task<IReadOnlyList<OrchestrationTask>> ListEnabledTasksByCapTopicAsync(string triggerTopic, CancellationToken cancellationToken);

    /// <summary>
    /// 新增任务聚合
    /// </summary>
    /// <param name="task">任务聚合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InsertTaskAsync(OrchestrationTask task, CancellationToken cancellationToken);

    /// <summary>
    /// 更新任务聚合
    /// </summary>
    /// <param name="task">任务聚合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateTaskAggregateAsync(OrchestrationTask task, CancellationToken cancellationToken);

    /// <summary>
    /// 删除任务聚合
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteTaskAsync(long id, CancellationToken cancellationToken);
}

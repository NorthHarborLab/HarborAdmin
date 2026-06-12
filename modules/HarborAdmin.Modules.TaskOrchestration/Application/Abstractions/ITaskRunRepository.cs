using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

/// <summary>
/// 编排任务运行仓储
/// </summary>
public interface ITaskRunRepository
{
    /// <summary>
    /// 判断任务是否存在运行中的实例
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在运行中实例</returns>
    Task<bool> HasRunningTaskAsync(long taskId, CancellationToken cancellationToken);

    /// <summary>
    /// 新增任务运行记录
    /// </summary>
    /// <param name="run">任务运行记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InsertRunAsync(OrchestrationTaskRun run, CancellationToken cancellationToken);

    /// <summary>
    /// 更新任务运行记录
    /// </summary>
    /// <param name="run">任务运行记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateRunAsync(OrchestrationTaskRun run, CancellationToken cancellationToken);

    /// <summary>
    /// 更新任务运行计数器
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="status">任务最终运行状态</param>
    /// <param name="startedAt">任务开始时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateTaskRunCountersAsync(long taskId, OrchestrationRunStatus status, DateTimeOffset? startedAt, CancellationToken cancellationToken);

    /// <summary>
    /// 新增节点运行记录
    /// </summary>
    /// <param name="run">节点运行记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InsertNodeRunAsync(OrchestrationNodeRun run, CancellationToken cancellationToken);

    /// <summary>
    /// 更新节点运行记录
    /// </summary>
    /// <param name="run">节点运行记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateNodeRunAsync(OrchestrationNodeRun run, CancellationToken cancellationToken);

    /// <summary>
    /// 分页列出指定任务运行记录
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="skip">跳过记录数</param>
    /// <param name="take">读取记录数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行记录集合</returns>
    Task<IReadOnlyList<OrchestrationTaskRun>> ListRunsAsync(long taskId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// 统计指定任务运行记录数
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行记录数</returns>
    Task<int> CountRunsAsync(long taskId, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询任务运行日志
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>运行日志分页结果</returns>
    Task<PagedResult<OrchestrationTaskRun>> QueryRunsAsync(QueryOrchestrationTaskRunRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 按 ID 获取任务运行记录
    /// </summary>
    /// <param name="runId">运行记录 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务运行记录</returns>
    Task<OrchestrationTaskRun?> GetRunAsync(long runId, CancellationToken cancellationToken);

    /// <summary>
    /// 列出指定任务运行的节点运行记录
    /// </summary>
    /// <param name="runId">运行记录 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点运行记录集合</returns>
    Task<IReadOnlyList<OrchestrationNodeRun>> ListNodeRunsAsync(long runId, CancellationToken cancellationToken);
}

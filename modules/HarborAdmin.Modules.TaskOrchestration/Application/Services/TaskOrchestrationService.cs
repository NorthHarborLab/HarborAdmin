using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Application.Execution;
using HarborAdmin.Modules.TaskOrchestration.Application.Events;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Services;

/// <summary>
/// 任务编排管理服务
/// </summary>
public sealed class TaskOrchestrationService(
    ITaskDefinitionRepository taskRepository,
    ITaskRunRepository runRepository,
    TaskDagValidator validator,
    TaskTriggerDispatcher dispatcher,
    ITaskCallableRegistry callableRegistry,
    IEventPublisher publisher,
    IHarborMapper mapper)
{
    /// <summary>
    /// 分页查询编排任务
    /// </summary>
    public async Task<PagedResult<OrchestrationTaskListItemDto>> QueryAsync(QueryOrchestrationTaskRequest request, CancellationToken cancellationToken)
    {
        var page = await taskRepository.QueryTasksAsync(request, cancellationToken);
        return PagedResult<OrchestrationTaskListItemDto>.From(page.Items.Select(mapper.Map<OrchestrationTaskListItemDto>).ToArray(), page.Total);
    }

    /// <summary>
    /// 获取编排任务详情
    /// </summary>
    public async Task<OrchestrationTaskDto> GetAsync(long id, CancellationToken cancellationToken) =>
        mapper.Map<OrchestrationTaskDto>(await GetRequiredTaskAsync(id, cancellationToken));

    /// <summary>
    /// 保存编排任务聚合
    /// </summary>
    public async Task<OrchestrationTaskDto> SaveAsync(long? id, SaveOrchestrationTaskRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var task = id.HasValue
            ? await GetRequiredTaskAsync(id.Value, cancellationToken)
            : new OrchestrationTask { CreatedAt = now };

        TaskDefinitionFactory.Apply(task, request, now);

        validator.Validate(task);
        if (id.HasValue)
        {
            await taskRepository.UpdateTaskAggregateAsync(task, cancellationToken);
        }
        else
        {
            await taskRepository.InsertTaskAsync(task, cancellationToken);
        }

        var saved = await GetRequiredTaskAsync(task.Id, cancellationToken);
        await PublishScheduleChangedAsync(saved.Id, false, cancellationToken);
        return mapper.Map<OrchestrationTaskDto>(saved);
    }

    /// <summary>
    /// 设置编排任务启停状态
    /// </summary>
    public async Task<OrchestrationTaskDto> SetEnabledAsync(long id, bool enabled, CancellationToken cancellationToken)
    {
        var task = await GetRequiredTaskAsync(id, cancellationToken);
        task.Enabled = enabled;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await taskRepository.UpdateTaskAggregateAsync(task, cancellationToken);
        await PublishScheduleChangedAsync(task.Id, false, cancellationToken);
        return mapper.Map<OrchestrationTaskDto>(task);
    }

    /// <summary>
    /// 删除编排任务
    /// </summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var task = await GetRequiredTaskAsync(id, cancellationToken);
        task.Enabled = false;
        await PublishScheduleChangedAsync(task.Id, true, cancellationToken);
        await taskRepository.DeleteTaskAsync(id, cancellationToken);
    }

    /// <summary>
    /// 手动运行编排任务
    /// </summary>
    public async Task<bool> RunAsync(long id, RunOrchestrationTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await GetRequiredTaskAsync(id, cancellationToken);
        await dispatcher.DispatchManualAsync(task, request, cancellationToken);
        return true;
    }

    /// <summary>
    /// 查询指定任务运行日志
    /// </summary>
    public async Task<PagedResult<OrchestrationTaskRunDto>> ListRunsAsync(long taskId, HarborQueryOptions request, CancellationToken cancellationToken)
    {
        var total = await runRepository.CountRunsAsync(taskId, cancellationToken);
        var runs = await runRepository.ListRunsAsync(taskId, request.Skip, request.PageSize, cancellationToken);
        return PagedResult<OrchestrationTaskRunDto>.From(runs.Select(mapper.Map<OrchestrationTaskRunDto>).ToArray(), total);
    }

    /// <summary>
    /// 查询任务运行日志
    /// </summary>
    public async Task<PagedResult<OrchestrationTaskRunDto>> QueryRunsAsync(QueryOrchestrationTaskRunRequest request, CancellationToken cancellationToken)
    {
        var page = await runRepository.QueryRunsAsync(request, cancellationToken);
        return PagedResult<OrchestrationTaskRunDto>.From(page.Items.Select(mapper.Map<OrchestrationTaskRunDto>).ToArray(), page.Total);
    }

    /// <summary>
    /// 获取运行日志详情
    /// </summary>
    public async Task<OrchestrationTaskRunDto> GetRunAsync(long runId, CancellationToken cancellationToken)
    {
        var run = await runRepository.GetRunAsync(runId, cancellationToken) ?? throw new NotFoundDomainException("运行记录不存在");
        var nodes = await runRepository.ListNodeRunsAsync(runId, cancellationToken);
        var dto = mapper.Map<OrchestrationTaskRunDto>(run);
        return dto with { Nodes = nodes.Select(mapper.Map<OrchestrationNodeRunDto>).ToArray() };
    }

    /// <summary>
    /// 列出已注册的接口调用方法
    /// </summary>
    public IReadOnlyList<TaskCallableDescriptorDto> ListCallables() =>
        callableRegistry.List()
            .Select(mapper.Map<TaskCallableDescriptorDto>)
            .ToArray();

    /// <summary>
    /// 获取必需存在的编排任务聚合
    /// </summary>
    private async Task<OrchestrationTask> GetRequiredTaskAsync(long id, CancellationToken cancellationToken) =>
        await taskRepository.GetTaskAsync(id, cancellationToken) ?? throw new NotFoundDomainException("任务不存在");

    /// <summary>
    /// 发布调度变更事件
    /// </summary>
    /// <param name="taskId">任务标识</param>
    /// <param name="deleted">是否删除</param>
    /// <param name="cancellationToken">取消令牌</param>
    private Task PublishScheduleChangedAsync(long taskId, bool deleted, CancellationToken cancellationToken) =>
        publisher.PublishAsync(TaskOrchestrationEventTopics.ScheduleChanged, new TaskScheduleChangedIntegrationEvent(taskId, deleted), cancellationToken);

}

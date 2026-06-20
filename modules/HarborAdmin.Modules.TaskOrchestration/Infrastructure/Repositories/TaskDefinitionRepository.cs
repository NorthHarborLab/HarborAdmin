using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;
using HarborAdmin.Modules.TaskOrchestration.Infrastructure.Contexts;
using FreeSql;

namespace HarborAdmin.Modules.TaskOrchestration.Infrastructure.Repositories;

/// <summary>
/// FreeSql 任务定义仓储
/// </summary>
public sealed class TaskDefinitionRepository(ITaskOrchestrationDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<ITaskOrchestrationDbContext>(db, unitOfWorkManager), ITaskDefinitionRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<OrchestrationTask>> QueryTasksAsync(QueryOrchestrationTaskRequest request, CancellationToken cancellationToken)
    {
        var query = FreeSql.Select<OrchestrationTask>()
            .IncludeMany(task => task.Triggers)
            .IncludeMany(task => task.Nodes)
            .IncludeMany(task => task.Edges);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(task => task.TaskCode.Contains(keyword) || task.Name.Contains(keyword));
        }

        if (request.Enabled.HasValue)
        {
            query = query.Where(task => task.Enabled == request.Enabled.Value);
        }

        var total = (int)await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(task => task.UpdatedAt ?? task.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return PagedResult<OrchestrationTask>.From(items, total);
    }

    /// <inheritdoc />
    public async Task<OrchestrationTask?> GetTaskAsync(long id, CancellationToken cancellationToken) =>
        await SelectTaskAggregate()
            .Where(task => task.Id == id)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<OrchestrationTask?> GetTaskByCodeAsync(string taskCode, CancellationToken cancellationToken) =>
        await SelectTaskAggregate()
            .Where(task => task.TaskCode == taskCode)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrchestrationTask>> ListEnabledTasksAsync(CancellationToken cancellationToken) =>
        SelectTaskAggregate()
            .Where(task => task.Enabled)
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<OrchestrationTask>>(task => task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrchestrationTask>> ListEnabledTasksByCapTopicAsync(string triggerTopic, CancellationToken cancellationToken) =>
        SelectTaskAggregate()
            .Where(task => task.Enabled && task.Triggers.Any(trigger =>
                trigger.Enabled && trigger.TriggerType == OrchestrationTriggerType.Cap && trigger.TriggerTopic == triggerTopic))
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<OrchestrationTask>>(task => task.Result, cancellationToken);

    /// <inheritdoc />
    public async Task InsertTaskAsync(OrchestrationTask task, CancellationToken cancellationToken)
    {
        await InsertAndFillIdAsync(task, cancellationToken);
        await InsertChildrenAsync(task, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateTaskAggregateAsync(OrchestrationTask task, CancellationToken cancellationToken)
    {
        await FreeSql.Update<OrchestrationTask>()
            .Set(item => item.TaskCode, task.TaskCode)
            .Set(item => item.Name, task.Name)
            .Set(item => item.Description, task.Description)
            .Set(item => item.Enabled, task.Enabled)
            .Set(item => item.AllowConcurrentRuns, task.AllowConcurrentRuns)
            .Set(item => item.DefaultParamsJson, task.DefaultParamsJson)
            .Set(item => item.ParamSchemaJson, task.ParamSchemaJson)
            .Set(item => item.UpdatedAt, task.UpdatedAt)
            .Set(item => item.UpdatedBy, task.UpdatedBy)
            .Where(item => item.Id == task.Id)
            .ExecuteAffrowsAsync(cancellationToken);
        await DeleteChildrenAsync(task.Id, cancellationToken);
        await InsertChildrenAsync(task, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteTaskAsync(long id, CancellationToken cancellationToken)
    {
        await DeleteChildrenAsync(id, cancellationToken);
        await FreeSql.Delete<OrchestrationTask>().Where(task => task.Id == id).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <summary>
    /// 创建任务聚合查询
    /// </summary>
    /// <returns>任务聚合查询</returns>
    private ISelect<OrchestrationTask> SelectTaskAggregate() =>
        FreeSql.Select<OrchestrationTask>()
            .IncludeMany(task => task.Triggers)
            .IncludeMany(task => task.Nodes)
            .IncludeMany(task => task.Edges);

    /// <summary>
    /// 保存任务子集合
    /// </summary>
    private async Task InsertChildrenAsync(OrchestrationTask task, CancellationToken cancellationToken)
    {
        foreach (var trigger in task.Triggers)
        {
            trigger.TaskId = task.Id;
        }
        foreach (var node in task.Nodes)
        {
            node.TaskId = task.Id;
        }
        foreach (var edge in task.Edges)
        {
            edge.TaskId = task.Id;
        }

        if (task.Triggers.Count > 0)
        {
            await FreeSql.Insert(task.Triggers).ExecuteAffrowsAsync(cancellationToken);
        }
        if (task.Nodes.Count > 0)
        {
            await FreeSql.Insert(task.Nodes).ExecuteAffrowsAsync(cancellationToken);
        }
        if (task.Edges.Count > 0)
        {
            await FreeSql.Insert(task.Edges).ExecuteAffrowsAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 删除任务子集合
    /// </summary>
    private async Task DeleteChildrenAsync(long taskId, CancellationToken cancellationToken)
    {
        await FreeSql.Delete<OrchestrationTaskTrigger>().Where(item => item.TaskId == taskId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<OrchestrationTaskNode>().Where(item => item.TaskId == taskId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<OrchestrationTaskEdge>().Where(item => item.TaskId == taskId).ExecuteAffrowsAsync(cancellationToken);
    }
}

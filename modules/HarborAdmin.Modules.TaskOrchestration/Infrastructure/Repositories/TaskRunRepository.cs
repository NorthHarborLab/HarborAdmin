using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;
using HarborAdmin.Modules.TaskOrchestration.Infrastructure.Contexts;

namespace HarborAdmin.Modules.TaskOrchestration.Infrastructure.Repositories;

/// <summary>
/// FreeSql 任务运行仓储
/// </summary>
public sealed class TaskRunRepository(ITaskOrchestrationDbContext db)
    : FreeSqlModuleRepository<ITaskOrchestrationDbContext>(db), ITaskRunRepository
{
    private static readonly IReadOnlyDictionary<string, PageDynamicField> RunFilterFields =
        new Dictionary<string, PageDynamicField>(StringComparer.OrdinalIgnoreCase)
        {
            ["taskId"] = new("taskId", nameof(OrchestrationTaskRun.TaskId), typeof(long)),
            ["taskCode"] = new("taskCode", nameof(OrchestrationTaskRun.TaskCode), typeof(string), [PageFilterOperator.Contains, PageFilterOperator.Eq]),
            ["status"] = new("status", nameof(OrchestrationTaskRun.Status), typeof(OrchestrationRunStatus)),
            ["triggerType"] = new("triggerType", nameof(OrchestrationTaskRun.TriggerType), typeof(string)),
            ["correlationId"] = new("correlationId", nameof(OrchestrationTaskRun.CorrelationId), typeof(string), [PageFilterOperator.Contains, PageFilterOperator.Eq]),
            ["createdAt"] = new("createdAt", nameof(OrchestrationTaskRun.CreatedAt), typeof(DateTimeOffset), [PageFilterOperator.Between, PageFilterOperator.Gte, PageFilterOperator.Lte]),
        };

    private static readonly IReadOnlyDictionary<string, string> RunSortFields =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = nameof(OrchestrationTaskRun.Id),
            ["createdAt"] = nameof(OrchestrationTaskRun.CreatedAt),
            ["finishedAt"] = nameof(OrchestrationTaskRun.FinishedAt),
            ["status"] = nameof(OrchestrationTaskRun.Status),
            ["triggerType"] = nameof(OrchestrationTaskRun.TriggerType),
        };

    /// <inheritdoc />
    public Task<bool> HasRunningTaskAsync(long taskId, CancellationToken cancellationToken) =>
        FreeSql.Select<OrchestrationTaskRun>()
            .AnyAsync(run => run.TaskId == taskId && run.Status == OrchestrationRunStatus.Running, cancellationToken);

    /// <inheritdoc />
    public Task InsertRunAsync(OrchestrationTaskRun run, CancellationToken cancellationToken) =>
        InsertAndFillIdAsync(run, cancellationToken);

    /// <inheritdoc />
    public Task UpdateRunAsync(OrchestrationTaskRun run, CancellationToken cancellationToken) =>
        FreeSql.Update<OrchestrationTaskRun>().SetSource(run).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateTaskRunCountersAsync(long taskId, OrchestrationRunStatus status, DateTimeOffset? startedAt, CancellationToken cancellationToken)
    {
        var update = FreeSql.Update<OrchestrationTask>()
            .Set(task => task.TotalRunCount + 1)
            .Set(task => task.LastStartedAt, startedAt)
            .Where(task => task.Id == taskId);

        update = status switch
        {
            OrchestrationRunStatus.Succeeded => update.Set(task => task.SucceededRunCount + 1),
            OrchestrationRunStatus.Failed => update.Set(task => task.FailedRunCount + 1),
            OrchestrationRunStatus.PartialFailed => update.Set(task => task.InterruptedRunCount + 1),
            _ => update,
        };

        return update.ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task InsertNodeRunAsync(OrchestrationNodeRun run, CancellationToken cancellationToken) =>
        InsertAndFillIdAsync(run, cancellationToken);

    /// <inheritdoc />
    public Task UpdateNodeRunAsync(OrchestrationNodeRun run, CancellationToken cancellationToken) =>
        FreeSql.Update<OrchestrationNodeRun>().SetSource(run).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrchestrationTaskRun>> ListRunsAsync(long taskId, int skip, int take, CancellationToken cancellationToken) =>
        FreeSql.Select<OrchestrationTaskRun>()
            .Where(run => run.TaskId == taskId)
            .OrderByDescending(run => run.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<OrchestrationTaskRun>>(task => task.Result, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountRunsAsync(long taskId, CancellationToken cancellationToken) =>
        FreeSql.Select<OrchestrationTaskRun>()
            .Where(run => run.TaskId == taskId)
            .CountAsync(cancellationToken)
            .ContinueWith(task => (int)task.Result, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<OrchestrationTaskRun>> QueryRunsAsync(QueryOrchestrationTaskRunRequest request, CancellationToken cancellationToken)
    {
        var query = FreeSql.Select<OrchestrationTaskRun>();
        if (request.TaskId.HasValue)
        {
            query = query.Where(run => run.TaskId == request.TaskId.Value);
        }

        query = query.ApplyDynamicFilters(request, RunFilterFields);

        var total = (int)await query.CountAsync(cancellationToken);
        var items = await query
            .ApplyDynamicSorting(request, RunSortFields, static current => current.OrderByDescending(run => run.CreatedAt))
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return PagedResult<OrchestrationTaskRun>.From(items, total);
    }

    /// <inheritdoc />
    public async Task<OrchestrationTaskRun?> GetRunAsync(long runId, CancellationToken cancellationToken) =>
        await FreeSql.Select<OrchestrationTaskRun>().Where(run => run.Id == runId).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrchestrationNodeRun>> ListNodeRunsAsync(long runId, CancellationToken cancellationToken) =>
        FreeSql.Select<OrchestrationNodeRun>()
            .Where(run => run.TaskRunId == runId)
            .OrderBy(run => run.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<OrchestrationNodeRun>>(task => task.Result, cancellationToken);
}

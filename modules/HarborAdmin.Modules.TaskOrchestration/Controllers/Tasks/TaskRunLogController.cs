using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;
using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.TaskOrchestration.Controllers.Tasks;

/// <summary>
/// 编排任务执行日志 API
/// </summary>
[ApiController]
[Route("api/admin/task-orchestration")]
public sealed class TaskRunLogController(TaskOrchestrationService service) : AdminControllerBase
{
    /// <summary>
    /// 查询指定任务运行日志
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="request">查询选项</param>
    /// <returns>任务运行日志分页结果</returns>
    [HttpPost("tasks/{taskId:long}/runs")]
    public Task<ApiResult<PagedResult<OrchestrationTaskRunDto>>> ListRuns(long taskId, [FromBody] HarborQueryOptions request) =>
        OkResultAsync(service.ListRunsAsync(taskId, request, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 查询任务运行日志
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>任务运行日志分页结果</returns>
    [HttpPost("runs/query")]
    public Task<ApiResult<PagedResult<OrchestrationTaskRunDto>>> QueryRuns([FromBody] QueryOrchestrationTaskRunRequest request) =>
        OkResultAsync(service.QueryRunsAsync(request, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 获取运行日志详情
    /// </summary>
    /// <param name="runId">运行记录 ID</param>
    /// <returns>运行日志详情</returns>
    [HttpGet("runs/{runId:long}")]
    public Task<ApiResult<OrchestrationTaskRunDto>> GetRun(long runId) =>
        OkResultAsync(service.GetRunAsync(runId, Request.HttpContext.RequestAborted));
}

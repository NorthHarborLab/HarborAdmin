using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.TaskOrchestration.Controllers.Tasks;

/// <summary>
/// 编排任务管理 API
/// </summary>
[ApiController]
[Route("api/admin/task-orchestration/tasks")]
public sealed class TaskManagementController(TaskOrchestrationService service) : AdminControllerBase
{
    /// <summary>
    /// 查询编排任务
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>编排任务分页结果</returns>
    [HttpPost("query")]
    public async Task<ApiResult<PagedResult<OrchestrationTaskListItemDto>>> Query([FromBody] QueryOrchestrationTaskRequest request) =>
        ApiResult.Ok(await service.QueryAsync(request, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 获取编排任务详情
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <returns>编排任务详情</returns>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<OrchestrationTaskDto>> Get(long id) =>
        ApiResult.Ok(await service.GetAsync(id, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 创建编排任务
    /// </summary>
    /// <param name="request">保存请求</param>
    /// <returns>创建后的编排任务详情</returns>
    [HttpPost]
    public async Task<ApiResult<OrchestrationTaskDto>> Create([FromBody] SaveOrchestrationTaskRequest request) =>
        ApiResult.Ok(await service.SaveAsync(null, request, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 更新编排任务
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <param name="request">保存请求</param>
    /// <returns>更新后的编排任务详情</returns>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<OrchestrationTaskDto>> Update(long id, [FromBody] SaveOrchestrationTaskRequest request) =>
        ApiResult.Ok(await service.SaveAsync(id, request, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 设置编排任务启停状态
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <param name="request">启停请求</param>
    /// <returns>更新后的编排任务详情</returns>
    [HttpPut("{id:long}/enabled")]
    public async Task<ApiResult<OrchestrationTaskDto>> SetEnabled(long id, [FromBody] SetOrchestrationTaskEnabledRequest request) =>
        ApiResult.Ok(await service.SetEnabledAsync(id, request.Enabled, Request.HttpContext.RequestAborted));

    /// <summary>
    /// 删除编排任务
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <returns>是否删除成功</returns>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id)
    {
        await service.DeleteAsync(id, Request.HttpContext.RequestAborted);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 手动运行编排任务
    /// </summary>
    /// <param name="id">任务 ID</param>
    /// <param name="request">运行请求</param>
    /// <returns>是否提交成功</returns>
    [HttpPost("{id:long}/run")]
    public async Task<ApiResult<bool>> Run(long id, [FromBody] RunOrchestrationTaskRequest request) =>
        ApiResult.Ok(await service.RunAsync(id, request, Request.HttpContext.RequestAborted));
}

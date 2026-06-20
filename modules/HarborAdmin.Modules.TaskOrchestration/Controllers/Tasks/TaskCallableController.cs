using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.TaskOrchestration.Controllers.Tasks;

/// <summary>
/// 编排任务可调用接口白名单 API
/// </summary>
[ApiController]
[Route("api/admin/task-orchestration/callables")]
public sealed class TaskCallableController(TaskOrchestrationService service) : HarborControllerBase
{
    /// <summary>
    /// 列出可调用接口方法
    /// </summary>
    /// <returns>可调用接口方法集合</returns>
    [HttpGet]
    public ApiResult<IReadOnlyList<TaskCallableDescriptorDto>> List() =>
        OkResult(service.ListCallables());
}

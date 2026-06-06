using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 调用日志 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/invocations")]
public sealed class AiInvocationsController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出调用日志。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiInvocationLogDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListInvocationLogsAsync(cancellationToken));
}




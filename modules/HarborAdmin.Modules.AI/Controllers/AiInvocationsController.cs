using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<IReadOnlyList<AiInvocationLogDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListInvocationLogsAsync(cancellationToken));
}



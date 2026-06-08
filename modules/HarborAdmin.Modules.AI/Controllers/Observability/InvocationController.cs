using HarborAdmin.Modules.AI.Application.Services.Observability;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.AI.Contracts.Observability.Dto;

namespace HarborAdmin.Modules.AI.Controllers.Observability;

/// <summary>
/// AI 调用日志 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/invocations")]
public sealed class InvocationController(AiObservabilityService service) : ControllerBase
{
    /// <summary>
    /// 列出调用日志。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiInvocationLogDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListInvocationLogsAsync(cancellationToken));
}

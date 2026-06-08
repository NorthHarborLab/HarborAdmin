using HarborAdmin.Modules.AI.Application.Services.Observability;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.AI.Contracts.Observability.Dto;

namespace HarborAdmin.Modules.AI.Controllers.Observability;

/// <summary>
/// AI 用量 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/usage")]
public sealed class UsageController(AiObservabilityService service) : ControllerBase
{
    /// <summary>
    /// 列出用量。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiUsageLedgerDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListUsageAsync(cancellationToken));
}

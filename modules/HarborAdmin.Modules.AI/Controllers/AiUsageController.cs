using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 用量 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/usage")]
public sealed class AiUsageController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出用量。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiUsageLedgerDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListUsageAsync(cancellationToken));
}

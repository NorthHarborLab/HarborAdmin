using HarborAdmin.Modules.AI.Application.Services.Observability;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.Observability.Dto;
using HarborAdmin.Modules.AI.Contracts.Observability.Request;

namespace HarborAdmin.Modules.AI.Controllers.Observability;

/// <summary>
/// AI 用量 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/usage")]
public sealed class UsageController(AiObservabilityService service) : AdminControllerBase
{
    /// <summary>
    /// 列出用量（兼容旧接口）。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiUsageLedgerDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListUsageAsync(cancellationToken));

    /// <summary>
    /// 获取用量概览 KPI。
    /// </summary>
    [HttpGet("overview")]
    public async Task<ApiResult<AiUsageOverviewDto>> Overview(
        [FromQuery] AiUsageSummaryQuery query,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetUsageOverviewAsync(query, cancellationToken));

    /// <summary>
    /// 分页获取用量聚合明细。
    /// </summary>
    [HttpGet("summary")]
    public async Task<ApiResult<PagedResult<AiUsageSummaryDto>>> Summary(
        [FromQuery] AiUsageSummaryQuery query,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.PageUsageSummaryAsync(query, cancellationToken));
}

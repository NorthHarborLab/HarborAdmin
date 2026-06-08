using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 供应商配额管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/providers/{providerId:long}/quota")]
public sealed class AiProviderQuotasController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 获取供应商限额。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<AiProviderQuotaDto?>> Get(long providerId, [FromQuery] string? producerKey, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetProviderQuotaAsync(providerId, producerKey, cancellationToken));

    /// <summary>
    /// 保存供应商限额。
    /// </summary>
    [HttpPut]
    public async Task<ApiResult<AiProviderQuotaDto>> Save(
        long providerId,
        [FromBody] SaveAiProviderQuotaRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveProviderQuotaAsync(providerId, request, cancellationToken));
}

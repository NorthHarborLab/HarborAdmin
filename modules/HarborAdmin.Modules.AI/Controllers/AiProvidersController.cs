using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 供应商管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/providers")]
public sealed class AiProvidersController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出供应商。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AiProviderDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListProvidersAsync(cancellationToken));

    /// <summary>
    /// 创建供应商。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AiProviderDto>> Create([FromBody] SaveAiProviderRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveProviderAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新供应商。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<AiProviderDto>> Update(long id, [FromBody] SaveAiProviderRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveProviderAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除供应商。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteProviderAsync(id, cancellationToken);
        return Ok(true);
    }

    /// <summary>
    /// 获取供应商限额。
    /// </summary>
    [HttpGet("{providerId:long}/quota")]
    public async Task<ActionResult<AiProviderQuotaDto?>> GetQuota(long providerId, [FromQuery] string? producerKey, CancellationToken cancellationToken) =>
        Ok(await service.GetProviderQuotaAsync(providerId, producerKey, cancellationToken));

    /// <summary>
    /// 保存供应商限额。
    /// </summary>
    [HttpPut("{providerId:long}/quota")]
    public async Task<ActionResult<AiProviderQuotaDto>> SaveQuota(
        long providerId,
        [FromBody] SaveAiProviderQuotaRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.SaveProviderQuotaAsync(providerId, request, cancellationToken));
}



using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<IReadOnlyList<AiUsageLedgerDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListUsageAsync(cancellationToken));

    /// <summary>
    /// 列出模型限额。
    /// </summary>
    [HttpGet("model-quotas")]
    public async Task<ActionResult<IReadOnlyList<AiModelQuotaDto>>> ListModelQuotas(CancellationToken cancellationToken) =>
        Ok(await service.ListModelQuotasAsync(cancellationToken));

    /// <summary>
    /// 创建模型限额。
    /// </summary>
    [HttpPost("model-quotas")]
    public async Task<ActionResult<AiModelQuotaDto>> CreateModelQuota([FromBody] SaveAiModelQuotaRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveModelQuotaAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新模型限额。
    /// </summary>
    [HttpPut("model-quotas/{id:long}")]
    public async Task<ActionResult<AiModelQuotaDto>> UpdateModelQuota(
        long id,
        [FromBody] SaveAiModelQuotaRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.SaveModelQuotaAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除模型限额。
    /// </summary>
    [HttpDelete("model-quotas/{id:long}")]
    public async Task<ActionResult<bool>> DeleteModelQuota(long id, CancellationToken cancellationToken)
    {
        await service.DeleteModelQuotaAsync(id, cancellationToken);
        return Ok(true);
    }
}



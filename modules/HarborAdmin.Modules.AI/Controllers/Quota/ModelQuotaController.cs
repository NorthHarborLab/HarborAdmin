using HarborAdmin.Modules.AI.Application.Services.Quota;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.AI.Contracts.Quota.Dto;
using HarborAdmin.Modules.AI.Contracts.Quota.Request;

namespace HarborAdmin.Modules.AI.Controllers.Quota;

/// <summary>
/// AI 模型配额管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/model-quotas")]
public sealed class ModelQuotaController(QuotaService service) : ControllerBase
{
    /// <summary>
    /// 列出模型限额。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiModelQuotaDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListModelQuotasAsync(cancellationToken));

    /// <summary>
    /// 创建模型限额。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiModelQuotaDto>> Create([FromBody] SaveAiModelQuotaRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveModelQuotaAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新模型限额。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiModelQuotaDto>> Update(long id, [FromBody] SaveAiModelQuotaRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveModelQuotaAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除模型限额。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteModelQuotaAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}

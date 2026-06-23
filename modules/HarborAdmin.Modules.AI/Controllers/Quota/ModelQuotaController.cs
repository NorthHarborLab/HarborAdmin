using HarborAdmin.Modules.AI.Application.Services.Quota;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.Quota.Dto;
using HarborAdmin.Modules.AI.Contracts.Quota.Request;

namespace HarborAdmin.Modules.AI.Controllers.Quota;

/// <summary>
/// AI 模型配额管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/model-quotas")]
public sealed class ModelQuotaController(QuotaService service) : AdminCrudControllerBase<AiModelQuotaDto, PageRequest, SaveAiModelQuotaRequest>
{
    /// <summary>
    /// 列出模型限额。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<PagedResult<AiModelQuotaDto>>> List([FromQuery] PageRequest query, CancellationToken cancellationToken) =>
        await PageResultAsync(query, service, cancellationToken);

    /// <summary>
    /// 获取模型限额详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<AiModelQuotaDto>> Get(long id, CancellationToken cancellationToken) =>
        await GetResultAsync(id, service, cancellationToken);

    /// <summary>
    /// 创建模型限额。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiModelQuotaDto>> Create([FromBody] SaveAiModelQuotaRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync(request, service, cancellationToken);

    /// <summary>
    /// 更新模型限额。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiModelQuotaDto>> Update(long id, [FromBody] SaveAiModelQuotaRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync(id, request, service, cancellationToken);

    /// <summary>
    /// 删除模型限额。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, service, cancellationToken);
}

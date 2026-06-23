using HarborAdmin.Modules.AI.Application.Services.Provider;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.Provider.Dto;
using HarborAdmin.Modules.AI.Contracts.Provider.Request;

namespace HarborAdmin.Modules.AI.Controllers.Provider;

/// <summary>
/// AI 供应商管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/providers")]
public sealed class ProviderController(ProviderService service) : AdminCrudControllerBase<AiProviderDto, PageRequest, SaveAiProviderRequest>
{
    /// <summary>
    /// 列出供应商。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<PagedResult<AiProviderDto>>> List([FromQuery] PageRequest query, CancellationToken cancellationToken) =>
        await PageResultAsync(query, service, cancellationToken);

    /// <summary>
    /// 获取供应商详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<AiProviderDto>> Get(long id, CancellationToken cancellationToken) =>
        await GetResultAsync(id, service, cancellationToken);

    /// <summary>
    /// 创建供应商。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiProviderDto>> Create([FromBody] SaveAiProviderRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync(request, service, cancellationToken);

    /// <summary>
    /// 更新供应商。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiProviderDto>> Update(long id, [FromBody] SaveAiProviderRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync(id, request, service, cancellationToken);

    /// <summary>
    /// 删除供应商。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, service, cancellationToken);
}

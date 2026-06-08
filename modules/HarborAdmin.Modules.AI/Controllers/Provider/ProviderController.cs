using HarborAdmin.Modules.AI.Application.Services.Provider;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.AI.Contracts.Provider.Dto;
using HarborAdmin.Modules.AI.Contracts.Provider.Request;

namespace HarborAdmin.Modules.AI.Controllers.Provider;

/// <summary>
/// AI 供应商管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/providers")]
public sealed class ProviderController(ProviderService service) : ControllerBase
{
    /// <summary>
    /// 列出供应商。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiProviderDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListProvidersAsync(cancellationToken));

    /// <summary>
    /// 创建供应商。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiProviderDto>> Create([FromBody] SaveAiProviderRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveProviderAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新供应商。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiProviderDto>> Update(long id, [FromBody] SaveAiProviderRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveProviderAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除供应商。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteProviderAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}

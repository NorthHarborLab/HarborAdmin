using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 业务管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/businesses")]
public sealed class AiBusinessesController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出业务。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiBusinessDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListBusinessesAsync(cancellationToken));

    /// <summary>
    /// 创建业务。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiBusinessDto>> Create([FromBody] SaveAiBusinessRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveBusinessAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新业务。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiBusinessDto>> Update(long id, [FromBody] SaveAiBusinessRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveBusinessAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除业务。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteBusinessAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}





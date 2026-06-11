using HarborAdmin.Modules.AI.Application.Services.Business;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.Business.Dto;
using HarborAdmin.Modules.AI.Contracts.Business.Request;

namespace HarborAdmin.Modules.AI.Controllers.Business;

/// <summary>
/// AI 业务管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/businesses")]
public sealed class BusinessController(BusinessService service) : CrudControllerBase<AiBusinessDto, SaveAiBusinessRequest>
{
    /// <summary>
    /// 列出业务。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiBusinessDto>>> List(CancellationToken cancellationToken) =>
        await ListResultAsync(service, cancellationToken);

    /// <summary>
    /// 获取业务详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<AiBusinessDto>> Get(long id, CancellationToken cancellationToken) =>
        await GetResultAsync(id, service, cancellationToken);

    /// <summary>
    /// 创建业务。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiBusinessDto>> Create([FromBody] SaveAiBusinessRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync(request, service, cancellationToken);

    /// <summary>
    /// 更新业务。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiBusinessDto>> Update(long id, [FromBody] SaveAiBusinessRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync(id, request, service, cancellationToken);

    /// <summary>
    /// 删除业务。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, service, cancellationToken);
}

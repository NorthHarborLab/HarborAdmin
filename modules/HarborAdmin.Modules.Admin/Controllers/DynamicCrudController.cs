using HarborAdmin.Modules.Admin.Application.Services;
using HarborAdmin.Modules.Admin.Application.Services.DynamicCurd;
using HarborAdmin.Modules.Admin.Contracts.DynamicCurd.Dtos;
using HarborAdmin.Modules.Admin.Contracts.DynamicCurd.Requests;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers;

/// <summary>
/// Admin 动态 CRUD 数据 API。
/// </summary>
[ApiController]
[Route("api/admin/dynamic-crud")]
public sealed class DynamicCrudController(AdminDynamicCrudService service) : ControllerBase
{
    /// <summary>
    /// 分页查询动态资源记录。
    /// </summary>
    [HttpPost("{featureCode}/query")]
    public async Task<ApiResult<DynamicQueryResultDto>> Query(
        string featureCode,
        [FromBody] DynamicQueryRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.QueryAsync(featureCode, request, cancellationToken));

    /// <summary>
    /// 获取动态资源记录详情。
    /// </summary>
    [HttpGet("{featureCode}/{id}")]
    public async Task<ApiResult<IReadOnlyDictionary<string, object?>>> Get(
        string featureCode,
        string id,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetAsync(featureCode, id, cancellationToken));

    /// <summary>
    /// 新增动态资源记录。
    /// </summary>
    [HttpPost("{featureCode}")]
    public async Task<ApiResult<IReadOnlyDictionary<string, object?>>> Create(
        string featureCode,
        [FromBody] Dictionary<string, object?> values,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.CreateAsync(featureCode, values, cancellationToken));

    /// <summary>
    /// 更新动态资源记录。
    /// </summary>
    [HttpPut("{featureCode}/{id}")]
    public async Task<ApiResult<IReadOnlyDictionary<string, object?>>> Update(
        string featureCode,
        string id,
        [FromBody] Dictionary<string, object?> values,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.UpdateAsync(featureCode, id, values, cancellationToken));

    /// <summary>
    /// 删除动态资源记录。
    /// </summary>
    [HttpDelete("{featureCode}/{id}")]
    public async Task<ApiResult<bool>> Delete(
        string featureCode,
        string id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(featureCode, id, cancellationToken);
        return ApiResult.Ok(true);
    }
}



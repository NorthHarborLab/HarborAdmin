using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers.FeatureDesign;

/// <summary>
/// Feature API 配置 API。
/// </summary>
[ApiController]
[Route("api/admin/feature-design/features/{featureCode}/apis")]
public sealed class FeatureDesignApiController(FeatureDesignApiService apiService) : ControllerBase
{
    private CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    /// <summary>
    /// 查询功能 API。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureApiDto>>> ListApis([FromRoute, Required] string featureCode) =>
        ApiResult.Ok(await apiService.ListApisAsync(featureCode, RequestCancellationToken));

    /// <summary>
    /// 新建功能 API。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AdminFeatureApiDto>> CreateApi(
        [FromRoute, Required] string featureCode,
        [FromBody] SaveAdminFeatureApiRequest request) =>
        ApiResult.Ok(await apiService.CreateApiAsync(featureCode, request, RequestCancellationToken));

    /// <summary>
    /// 更新功能 API。
    /// </summary>
    [HttpPut("{apiCode}")]
    public async Task<ApiResult<AdminFeatureApiDto>> UpdateApi(
        [FromRoute, Required] string featureCode,
        [FromRoute, Required] string apiCode,
        [FromBody] SaveAdminFeatureApiRequest request) =>
        ApiResult.Ok(await apiService.UpdateApiAsync(featureCode, apiCode, request, RequestCancellationToken));

    /// <summary>
    /// 删除功能 API。
    /// </summary>
    [HttpDelete("{apiCode}")]
    public async Task<ApiResult<bool>> DeleteApi(
        [FromRoute, Required] string featureCode,
        [FromRoute, Required] string apiCode)
    {
        await apiService.DeleteApiAsync(featureCode, apiCode, RequestCancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 生成默认 CRUD API。
    /// </summary>
    [HttpPost("generate-crud")]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureApiDto>>> GenerateCrudApis(
        [FromRoute, Required] string featureCode,
        [FromBody] GenerateCrudApisRequest request) =>
        ApiResult.Ok(await apiService.GenerateCrudApisAsync(featureCode, request, RequestCancellationToken));
}



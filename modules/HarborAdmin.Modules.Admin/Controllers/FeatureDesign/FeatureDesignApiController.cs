using System.ComponentModel.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Controllers;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.Modules.Admin.Controllers.FeatureDesign;

/// <summary>
/// Feature API 配置 API。
/// </summary>
[ApiController]
[Route("api/admin/feature-design/features/{featureCode}/apis")]
public sealed class FeatureDesignApiController(FeatureDesignApiService apiService) : HarborControllerBase
{
    private CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    /// <summary>
    /// 查询功能 API。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureApiDto>>> ListApis([FromRoute, Required] string featureCode) =>
        await OkResultAsync(apiService.ListApisAsync(featureCode, RequestCancellationToken));

    /// <summary>
    /// 查询全部功能 API 树。
    /// </summary>
    [HttpGet("/api/admin/feature-design/apis/tree")]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureApiTreeDto>>> ListApiTree() =>
        await OkResultAsync(apiService.ListApiTreeAsync(RequestCancellationToken));

    /// <summary>
    /// 新建功能 API。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AdminFeatureApiDto>> CreateApi(
        [FromRoute, Required] string featureCode,
        [FromBody] SaveAdminFeatureApiRequest request) =>
        await OkResultAsync(apiService.CreateApiAsync(featureCode, request, RequestCancellationToken));

    /// <summary>
    /// 更新功能 API。
    /// </summary>
    [HttpPut("{apiCode}")]
    public async Task<ApiResult<AdminFeatureApiDto>> UpdateApi(
        [FromRoute, Required] string featureCode,
        [FromRoute, Required] string apiCode,
        [FromBody] SaveAdminFeatureApiRequest request) =>
        await OkResultAsync(apiService.UpdateApiAsync(featureCode, apiCode, request, RequestCancellationToken));

    /// <summary>
    /// API 排序。
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ApiResult<bool>> ReorderApis(
        [FromRoute, Required] string featureCode,
        [FromBody] ReorderAdminFeatureApiRequest request)
    {
        await apiService.ReorderApisAsync(featureCode, request, RequestCancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 删除功能 API。
    /// </summary>
    [HttpDelete("{apiCode}")]
    public async Task<ApiResult<bool>> DeleteApi(
        [FromRoute, Required] string featureCode,
        [FromRoute, Required] string apiCode)
    {
        await apiService.DeleteApiAsync(featureCode, apiCode, RequestCancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 生成默认 CRUD API。
    /// </summary>
    [HttpPost("generate-crud")]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureApiDto>>> GenerateCrudApis(
        [FromRoute, Required] string featureCode,
        [FromBody] GenerateCrudApisRequest request) =>
        await OkResultAsync(apiService.GenerateCrudApisAsync(featureCode, request, RequestCancellationToken));
}


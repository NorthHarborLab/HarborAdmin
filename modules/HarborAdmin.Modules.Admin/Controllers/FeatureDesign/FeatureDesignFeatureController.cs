using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers.FeatureDesign;

/// <summary>
/// Feature 设计基础配置 API。
/// </summary>
[ApiController]
[Route("api/admin/feature-design/features")]
public sealed class FeatureDesignFeatureController(FeatureDesignFeatureService featureService) : ControllerBase
{
    private CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    /// <summary>
    /// 查询 Feature 列表。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureDto>>> ListFeatures() =>
        ApiResult.Ok(await featureService.ListFeaturesAsync(RequestCancellationToken));

    /// <summary>
    /// 新建 Feature。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AdminFeatureDto>> CreateFeature([FromBody] SaveAdminFeatureRequest request) =>
        ApiResult.Ok(await featureService.CreateFeatureAsync(request, RequestCancellationToken));

    /// <summary>
    /// 更新 Feature。
    /// </summary>
    [HttpPut("{featureCode}")]
    public async Task<ApiResult<AdminFeatureDto>> UpdateFeature([FromRoute, Required] string featureCode, [FromBody] SaveAdminFeatureRequest request) =>
        ApiResult.Ok(await featureService.UpdateFeatureAsync(featureCode, request, RequestCancellationToken));

    /// <summary>
    /// 删除 Feature。
    /// </summary>
    [HttpDelete("{featureCode}")]
    public async Task<ApiResult<bool>> DeleteFeature([FromRoute, Required] string featureCode)
    {
        await featureService.DeleteFeatureAsync(featureCode, RequestCancellationToken);
        return ApiResult.Ok(true);
    }
}



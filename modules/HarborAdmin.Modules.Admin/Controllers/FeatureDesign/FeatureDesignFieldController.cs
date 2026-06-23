using System.ComponentModel.DataAnnotations;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.Modules.Admin.Controllers.FeatureDesign;

/// <summary>
/// Feature 字段配置 API。
/// </summary>
[ApiController]
[Route("api/admin/feature-design/features/{featureCode}/fields")]
public sealed class FeatureDesignFieldController(FeatureDesignFieldService fieldService) : AdminControllerBase
{
    private CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    /// <summary>
    /// 查询功能字段。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureFieldDto>>> ListFields([FromRoute, Required] string featureCode) =>
        await OkResultAsync(fieldService.ListFieldsAsync(featureCode, RequestCancellationToken));

    /// <summary>
    /// 新建功能字段。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AdminFeatureFieldDto>> CreateField(
        [FromRoute, Required] string featureCode,
        [FromBody] SaveAdminFeatureFieldRequest request) =>
        await OkResultAsync(fieldService.CreateFieldAsync(featureCode, request, RequestCancellationToken));

    /// <summary>
    /// 更新功能字段。
    /// </summary>
    [HttpPut("{fieldCode}")]
    public async Task<ApiResult<AdminFeatureFieldDto>> UpdateField(
        [FromRoute, Required] string featureCode,
        [FromRoute, Required] string fieldCode,
        [FromBody] SaveAdminFeatureFieldRequest request) =>
        await OkResultAsync(fieldService.UpdateFieldAsync(featureCode, fieldCode, request, RequestCancellationToken));

    /// <summary>
    /// 字段排序。
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ApiResult<bool>> ReorderFields(
        [FromRoute, Required] string featureCode,
        [FromBody] ReorderAdminFeatureFieldRequest request)
    {
        await fieldService.ReorderFieldsAsync(featureCode, request, RequestCancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 删除功能字段。
    /// </summary>
    [HttpDelete("{fieldCode}")]
    public async Task<ApiResult<bool>> DeleteField(
        [FromRoute, Required] string featureCode,
        [FromRoute, Required] string fieldCode)
    {
        await fieldService.DeleteFieldAsync(featureCode, fieldCode, RequestCancellationToken);
        return OkResult(true);
}
}


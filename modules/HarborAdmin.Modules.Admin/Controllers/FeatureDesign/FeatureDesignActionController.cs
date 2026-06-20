using System.ComponentModel.DataAnnotations;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.Modules.Admin.Controllers.FeatureDesign;

/// <summary>
/// Feature Action 配置 API。
/// </summary>
[ApiController]
[Route("api/admin/feature-design/features/{featureCode}/actions")]
public sealed class FeatureDesignActionController(FeatureDesignActionService actionService) : HarborControllerBase
{
    private CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    /// <summary>
    /// 查询功能动作。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AdminFeatureActionDto>>> ListActions([FromRoute, Required] string featureCode) =>
        await OkResultAsync(actionService.ListActionsAsync(featureCode, RequestCancellationToken));

    /// <summary>
    /// 新建功能动作。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AdminFeatureActionDto>> CreateAction([FromRoute, Required] string featureCode,
        [FromBody] SaveAdminFeatureActionRequest request) =>
        await OkResultAsync(actionService.CreateActionAsync(featureCode, request, RequestCancellationToken));

    /// <summary>
    /// 更新功能动作。
    /// </summary>
    [HttpPut("{actionCode}")]
    public async Task<ApiResult<AdminFeatureActionDto>> UpdateAction([FromRoute, Required] string featureCode, [FromRoute, Required] string actionCode,
        [FromBody] SaveAdminFeatureActionRequest request) =>
        await OkResultAsync(actionService.UpdateActionAsync(featureCode, actionCode, request, RequestCancellationToken));

    /// <summary>
    /// 权限点排序。
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ApiResult<bool>> ReorderActions(
        [FromRoute, Required] string featureCode,
        [FromBody] ReorderAdminFeatureActionRequest request)
    {
        await actionService.ReorderActionsAsync(featureCode, request, RequestCancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 删除功能动作。
    /// </summary>
    [HttpDelete("{actionCode}")]
    public async Task<ApiResult<bool>> DeleteAction([FromRoute, Required] string featureCode, [FromRoute, Required] string actionCode)
    {
        await actionService.DeleteActionAsync(featureCode, actionCode, RequestCancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 保存动作 API 绑定。
    /// </summary>
    [HttpPut("{actionCode}/apis")]
    public async Task<ApiResult<AdminFeatureActionDto>> SaveActionApis([FromRoute, Required] string featureCode,
        [FromRoute, Required] string actionCode,
        [FromBody, Required] IReadOnlyList<long> apiIds) =>
        await OkResultAsync(actionService.SaveActionApisAsync(featureCode, actionCode, apiIds, RequestCancellationToken));
}


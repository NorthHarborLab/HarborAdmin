using HarborAdmin.Modules.ConfigCenter.Application.Services;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.ConfigCenter.Controllers;

/// <summary>
/// 配置中心应用（AppId）管理 API。
/// </summary>
/// <param name="service">配置中心应用服务。</param>
[ApiController]
[Route("api/admin/config-center/apps")]
public sealed class ConfigCenterAppsController(ConfigCenterApplicationService service) : ControllerBase
{
    /// <summary>列出所有已注册应用。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用列表。</returns>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<ConfigApplicationDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListApplicationsAsync(cancellationToken));

    /// <summary>注册新应用。</summary>
    /// <param name="request">创建请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已创建的应用。</returns>
    [HttpPost]
    public async Task<ApiResult<ConfigApplicationDto>> Create(
        [FromBody] CreateConfigApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateApplicationAsync(request, cancellationToken);
        return ApiResult.Ok(created);
    }

    /// <summary>
    /// 更新应用元数据
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="request">更新请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的应用。</returns>
    [HttpPut("{appId}")]
    public async Task<ApiResult<ConfigApplicationDto>> Update(string appId, [FromBody] UpdateConfigApplicationRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.UpdateApplicationAsync(appId, request, cancellationToken));

    /// <summary>删除应用及其全部配置数据。</summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpDelete("{appId}")]
    public async Task<ApiResult<bool>> Delete(string appId, CancellationToken cancellationToken)
    {
        await service.DeleteApplicationAsync(appId, cancellationToken);
        return ApiResult.Ok(true);
    }
}



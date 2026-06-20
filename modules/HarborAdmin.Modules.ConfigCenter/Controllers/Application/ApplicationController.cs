using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.Modules.ConfigCenter.Contracts.Application.Dto;
using HarborAdmin.Modules.ConfigCenter.Contracts.Application.Request;

namespace HarborAdmin.Modules.ConfigCenter.Controllers.Application;

/// <summary>
/// 配置中心应用（AppId）管理 API。
/// </summary>
/// <param name="service">配置中心应用服务。</param>
[ApiController]
[Route("api/admin/config-center/apps")]
public sealed class ApplicationController(ConfigCenterApplicationService service) : HarborControllerBase
{
    /// <summary>
    /// 列出所有已注册应用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用列表。</returns>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<ConfigApplicationDto>>> List(CancellationToken cancellationToken) =>
        await ListResultAsync(cancellationToken, service.ListApplicationsAsync);

    /// <summary>
    /// 注册新应用。
    /// </summary>
    /// <param name="request">创建请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已创建的应用。</returns>
    [HttpPost]
    public async Task<ApiResult<ConfigApplicationDto>> Create(
        [FromBody] SaveConfigApplicationRequest request,
        CancellationToken cancellationToken) =>
        await CreateResultAsync<SaveConfigApplicationRequest, ConfigApplicationDto>(request, cancellationToken, (body, token) => service.SaveApplicationAsync(null, body, token));

    /// <summary>
    /// 更新应用元数据。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="request">更新请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的应用。</returns>
    [HttpPut("{appId}")]
    public async Task<ApiResult<ConfigApplicationDto>> Update(
        string appId,
        [FromBody] SaveConfigApplicationRequest request,
        CancellationToken cancellationToken) =>
        await UpdateResultAsync<string, SaveConfigApplicationRequest, ConfigApplicationDto>(appId, request, cancellationToken, service.SaveApplicationAsync);

    /// <summary>
    /// 删除应用及其全部配置数据。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpDelete("{appId}")]
    public async Task<ApiResult<bool>> Delete(string appId, CancellationToken cancellationToken) =>
        await DeleteResultAsync(appId, cancellationToken, service.DeleteApplicationAsync);
}

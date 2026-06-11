using HarborAdmin.BuildingBlocks.Abstractions.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.Modules.ConfigCenter.Contracts.Item.Dto;
using HarborAdmin.Modules.ConfigCenter.Contracts.Item.Request;

namespace HarborAdmin.Modules.ConfigCenter.Controllers.Item;

/// <summary>
/// 指定应用下的草稿配置项 CRUD API。
/// </summary>
/// <param name="service">配置中心应用服务。</param>
[ApiController]
[Route("api/admin/config-center/{appId}/items")]
public sealed class ItemController(ConfigCenterItemService service) : HarborControllerBase
{
    /// <summary>
    /// 列出草稿配置项。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>草稿配置项列表。</returns>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<ConfigItemDto>>> List(string appId, CancellationToken cancellationToken) =>
        await OkResultAsync(service.ListItemsAsync(appId, cancellationToken));

    /// <summary>
    /// 新增草稿配置项。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="request">创建请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已创建的配置项。</returns>
    [HttpPost]
    public async Task<ApiResult<ConfigItemDto>> Create(string appId, [FromBody] SaveConfigItemRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(service.SaveItemAsync(appId, null, request, cancellationToken));

    /// <summary>
    /// 更新草稿配置项。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="id">配置项主键。</param>
    /// <param name="request">更新请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的配置项。</returns>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<ConfigItemDto>> Update(string appId, long id, [FromBody] SaveConfigItemRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(service.SaveItemAsync(appId, id, request, cancellationToken));

    /// <summary>
    /// 删除草稿配置项。
    /// </summary>
    /// <param name="id">配置项主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, cancellationToken, service.DeleteItemAsync);
}

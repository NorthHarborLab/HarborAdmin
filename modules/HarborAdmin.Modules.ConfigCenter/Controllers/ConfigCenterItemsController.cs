using HarborAdmin.Modules.ConfigCenter.Application.Services;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.ConfigCenter.Controllers;

/// <summary>
/// 指定应用下的草稿配置项 CRUD API。
/// </summary>
/// <param name="service">配置中心应用服务。</param>
[ApiController]
[Route("api/admin/config-center/{appId}/items")]
public sealed class ConfigCenterItemsController(ConfigCenterService service) : ControllerBase
{
    /// <summary>
    /// 列出草稿配置项。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>草稿配置项列表。</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConfigItemDto>>> List(
        string appId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListItemsAsync(appId, cancellationToken));

    /// <summary>
    /// 新增草稿配置项。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="request">创建请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已创建的配置项。</returns>
    [HttpPost]
    public async Task<ActionResult<ConfigItemDto>> Create(
        string appId,
        [FromBody] CreateConfigItemRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateItemAsync(appId, request, cancellationToken);
        return CreatedAtAction(nameof(List), new { appId }, created);
    }

    /// <summary>
    /// 更新草稿配置项。
    /// </summary>
    /// <param name="id">配置项主键。</param>
    /// <param name="request">更新请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的配置项。</returns>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ConfigItemDto>> Update(
        long id,
        [FromBody] UpdateConfigItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateItemAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除草稿配置项。
    /// </summary>
    /// <param name="id">配置项主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteItemAsync(id, cancellationToken);
        return Ok(true);
    }
}

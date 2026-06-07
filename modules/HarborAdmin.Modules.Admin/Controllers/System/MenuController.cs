using HarborAdmin.Modules.Admin.Application.Services.Menu;
using HarborAdmin.Modules.Admin.Contracts.System;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统菜单管理。
/// </summary>
[ApiController]
[Route("api/admin/system/menu")]
public sealed class MenuController(MenuService menuService) : ControllerBase
{
    /// <summary>
    /// 查询菜单树。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<IReadOnlyList<SystemMenuDto>>> List(
        [FromQuery] bool includePermissions,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(includePermissions
            ? await menuService.ListMenuPermissionTreeAsync(cancellationToken)
            : await menuService.ListMenusAsync(cancellationToken));

    /// <summary>
    /// 菜单名称是否存在。
    /// </summary>
    [HttpGet("name-exists")]
    public async Task<ApiResult<bool>> NameExists([FromQuery] string name, [FromQuery] long? id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await menuService.MenuNameExistsAsync(name, id, cancellationToken));

    /// <summary>
    /// 菜单路径是否存在。
    /// </summary>
    [HttpGet("path-exists")]
    public async Task<ApiResult<bool>> PathExists([FromQuery] string path, [FromQuery] long? id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await menuService.MenuPathExistsAsync(path, id, cancellationToken));

    /// <summary>
    /// 创建菜单。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemMenuDto>> Create([FromBody] SaveSystemMenuRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await menuService.SaveMenuAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新菜单。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<SystemMenuDto>> Update(long id, [FromBody] SaveSystemMenuRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await menuService.SaveMenuAsync(id, request, cancellationToken));

    /// <summary>
    /// 同级菜单排序。
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ApiResult<bool>> Reorder([FromBody] ReorderSystemMenuRequest request, CancellationToken cancellationToken)
    {
        await menuService.ReorderMenusAsync(request, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 删除菜单。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await menuService.DeleteMenuAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}

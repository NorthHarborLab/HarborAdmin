using HarborAdmin.Modules.Admin.Application.Services.Menu;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统菜单管理。
/// </summary>
[ApiController]
[Route("api/admin/system/menu")]
public sealed class MenuController(MenuService menuService) : HarborControllerBase
{
    /// <summary>
    /// 查询菜单树。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<IReadOnlyList<SystemMenuDto>>> List(
        [FromQuery] bool includePermissions,
        CancellationToken cancellationToken) =>
        OkResult(includePermissions
            ? await menuService.ListMenuPermissionTreeAsync(cancellationToken)
            : await menuService.ListMenusAsync(cancellationToken));

    /// <summary>
    /// 菜单名称是否存在。
    /// </summary>
    [HttpGet("name-exists")]
    public async Task<ApiResult<bool>> NameExists(
        [FromQuery] string name,
        [FromQuery] string? id,
        CancellationToken cancellationToken) =>
        await OkResultAsync(menuService.MenuNameExistsAsync(name, ParseMenuId(id), cancellationToken));

    /// <summary>
    /// 菜单路径是否存在。
    /// </summary>
    [HttpGet("path-exists")]
    public async Task<ApiResult<bool>> PathExists(
        [FromQuery] string path,
        [FromQuery] string? id,
        CancellationToken cancellationToken) =>
        await OkResultAsync(menuService.MenuPathExistsAsync(path, ParseMenuId(id), cancellationToken));

    /// <summary>
    /// 解析菜单 ID 查询参数。
    /// </summary>
    private static long? ParseMenuId(string? id) =>
        long.TryParse(id, out var menuId) ? menuId : null;

    /// <summary>
    /// 创建菜单。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemMenuDto>> Create([FromBody] SaveSystemMenuRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(menuService.SaveMenuAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新菜单。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<SystemMenuDto>> Update(long id, [FromBody] SaveSystemMenuRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(menuService.SaveMenuAsync(id, request, cancellationToken));

    /// <summary>
    /// 同级菜单排序。
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ApiResult<bool>> Reorder([FromBody] ReorderSystemMenuRequest request, CancellationToken cancellationToken)
    {
        await menuService.ReorderMenusAsync(request, cancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 删除菜单。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await menuService.DeleteMenuAsync(id, cancellationToken);
        return OkResult(true);
    }
}

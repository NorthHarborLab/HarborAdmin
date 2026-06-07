using HarborAdmin.Modules.Admin.Application.Services.Role;
using HarborAdmin.Modules.Admin.Contracts.System;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统角色管理。
/// </summary>
[ApiController]
[Route("api/admin/system/role")]
public sealed class RoleController(RoleService roleService) : ControllerBase
{
    /// <summary>
    /// 查询角色列表。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<IReadOnlyList<SystemRoleDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await roleService.ListRolesAsync(cancellationToken));

    /// <summary>
    /// 创建角色。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemRoleDto>> Create([FromBody] SaveSystemRoleRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await roleService.SaveRoleAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新角色。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<SystemRoleDto>> Update(long id, [FromBody] SaveSystemRoleRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await roleService.SaveRoleAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除角色。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await roleService.DeleteRoleAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}

using HarborAdmin.Modules.Admin.Application.Services.Role;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统角色管理。
/// </summary>
[ApiController]
[Route("api/admin/system/role")]
public sealed class RoleController(RoleService roleService) : CrudControllerBase<SystemRoleDto, PageRequest, SaveSystemRoleRequest>
{
    /// <summary>
    /// 查询角色列表。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<PagedResult<SystemRoleDto>>> List([FromQuery] PageRequest query, CancellationToken cancellationToken) =>
        await PageResultAsync(query, roleService, cancellationToken);

    /// <summary>
    /// 创建角色。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemRoleDto>> Create([FromBody] SaveSystemRoleRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync(request, roleService, cancellationToken);

    /// <summary>
    /// 更新角色。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<SystemRoleDto>> Update(long id, [FromBody] SaveSystemRoleRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync(id, request, roleService, cancellationToken);

    /// <summary>
    /// 删除角色。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, roleService, cancellationToken);
}

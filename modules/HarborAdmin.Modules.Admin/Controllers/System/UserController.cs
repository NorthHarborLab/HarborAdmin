using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统用户管理。
/// </summary>
[ApiController]
[Route("api/admin/system/user")]
public sealed class UserController(UserService userService, ICurrentUser currentUser) : HarborControllerBase
{
    /// <summary>
    /// 查询用户列表。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<IReadOnlyList<SystemUserDto>>> List([FromQuery] long? deptId, CancellationToken cancellationToken) =>
        await OkResultAsync(userService.ListUsersAsync(currentUser.Id, deptId, cancellationToken));

    /// <summary>
    /// 创建用户。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemUserDto>> Create([FromBody] SaveSystemUserRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(userService.SaveUserAsync(currentUser.Id, null, request, cancellationToken));

    /// <summary>
    /// 更新用户。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<SystemUserDto>> Update(long id, [FromBody] SaveSystemUserRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(userService.SaveUserAsync(currentUser.Id, id, request, cancellationToken));

    /// <summary>
    /// 删除用户。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await userService.DeleteUserAsync(id, cancellationToken);
        return OkResult(true);
    }
}

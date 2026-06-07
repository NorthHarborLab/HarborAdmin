using HarborAdmin.Modules.Admin.Application.Services.Session;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers;

/// <summary>
/// 前端菜单接口。
/// </summary>
[ApiController]
[Route("menu")]
public sealed class MenuController(SessionService sessionService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// 获取当前用户后端路由。
    /// </summary>
    [HttpGet("all")]
    public async Task<ApiResult<IReadOnlyList<BackendRouteDto>>> GetAll(CancellationToken cancellationToken)
    {
        var session = await sessionService.BuildSessionAsync(currentUser.Id, cancellationToken);
        return ApiResult.Ok(session.Routes);
    }
}



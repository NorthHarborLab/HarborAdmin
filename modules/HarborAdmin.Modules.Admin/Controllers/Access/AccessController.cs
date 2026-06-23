using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.Modules.Admin.Application.Services.Auth;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.Admin.Controllers.Access;

/// <summary>
/// 当前用户访问包接口（需 access token）。
/// </summary>
[ApiController]
[AuthenticatedOnly]
[Route("api/admin/access")]
public sealed class AccessController(SessionService sessionService, AuthService authService, ICurrentUser currentUser) : AdminControllerBase
{
    /// <summary>
    /// 获取当前用户信息。
    /// </summary>
    [HttpGet("me")]
    public async Task<ApiResult<CurrentUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var session = await sessionService.BuildSessionAsync(currentUser.Id, cancellationToken);
        return OkResult(session.User);
    }

    /// <summary>
    /// 获取会话访问包。
    /// </summary>
    [HttpGet("session")]
    public async Task<ApiResult<SessionSnapshotDto>> GetSession(CancellationToken cancellationToken) =>
        await OkResultAsync(sessionService.BuildSessionAsync(currentUser.Id, cancellationToken));

    /// <summary>
    /// 获取 sessionVersion。
    /// </summary>
    [HttpGet("session/version")]
    public async Task<ApiResult<SessionVersionDto>> GetSessionVersion(CancellationToken cancellationToken) =>
        await OkResultAsync(cancellationToken, sessionService.GetSessionVersionAsync);

    /// <summary>
    /// 获取权限码。
    /// </summary>
    [HttpGet("permissions")]
    public async Task<ApiResult<IReadOnlyList<string>>> GetPermissions(CancellationToken cancellationToken)
    {
        var session = await sessionService.BuildSessionAsync(currentUser.Id, cancellationToken);
        return OkResult(session.Permissions);
    }

    /// <summary>
    /// 退出登录。
    /// </summary>
    [HttpPost("logout")]
    public async Task<ApiResult<bool>> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(null, Request, Response, cancellationToken);
        return OkResult(true);
    }
}

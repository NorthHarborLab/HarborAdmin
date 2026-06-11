using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.Admin.Application.Services.Auth;
using HarborAdmin.Modules.Admin.Contracts.Auth.Dto;
using HarborAdmin.Modules.Admin.Contracts.Auth.Request;
using HarborAdmin.Modules.Admin.Contracts.Captcha.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.Admin.Controllers.Auth;

/// <summary>
/// Admin 认证接口（匿名访问：登录、刷新、验证码等）
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    private CancellationToken RequestCancellationToken => HttpContext.RequestAborted;

    /// <summary>
    /// 创建一次性 RSA 加密挑战
    /// </summary>
    [HttpPost("crypto-challenge")]
    public async Task<ApiResult<CryptoChallengeDto>> CreateCryptoChallenge() =>
        ApiResult.Ok(await authService.CreateCryptoChallengeAsync(RequestCancellationToken));

    /// <summary>
    /// 创建验证码挑战
    /// </summary>
    [HttpGet("captcha")]
    public async Task<ApiResult<CaptchaChallengeDto>> CreateCaptcha() =>
        ApiResult.Ok(await authService.CreateCaptchaAsync(RequestCancellationToken));

    /// <summary>
    /// 校验验证码
    /// </summary>
    [HttpPost("captcha/verify")]
    public async Task<ApiResult<VerifyCaptchaResult>> VerifyCaptcha([FromBody] VerifyCaptchaRequest request) =>
        ApiResult.Ok(await authService.VerifyCaptchaAsync(request, RequestCancellationToken));

    /// <summary>
    /// 登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ApiResult<LoginResultDto>> Login([FromBody] LoginRequest request) =>
        ApiResult.Ok(await authService.LoginAsync(request, Response, RequestCancellationToken));

    /// <summary>
    /// 刷新 access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ApiResult<RefreshTokenResultDto>> Refresh() =>
        ApiResult.Ok(await authService.RefreshAsync(Request, Response, RequestCancellationToken));
}

using HarborAdmin.Modules.Secrets.Application.Services;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.Secrets.Contracts.Secret.Dto;
using HarborAdmin.Modules.Secrets.Contracts.Secret.Request;

namespace HarborAdmin.Modules.Secrets.Controllers.Secret;

/// <summary>
/// 通用 Secret 管理 API。
/// </summary>
[ApiController]
[Route("api/admin/secrets")]
public sealed class SecretController(SecretService secretService) : ControllerBase
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<SecretDto>>> List() =>
        ApiResult.Ok(await secretService.ListAsync(HttpContext.RequestAborted));

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SecretDto>> Save([FromBody] SaveSecretRequest request) =>
        ApiResult.Ok(await secretService.SaveAsync(request, HttpContext.RequestAborted));

    /// <summary>
    /// 设置密钥启停状态。
    /// </summary>
    [HttpPut("enabled")]
    public async Task<ApiResult<SecretDto>> SetEnabled([FromBody] SetSecretEnabledRequest request) =>
        ApiResult.Ok(await secretService.SetEnabledAsync(request, HttpContext.RequestAborted));
}

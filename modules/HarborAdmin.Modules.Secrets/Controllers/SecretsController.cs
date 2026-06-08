using HarborAdmin.Modules.Secrets.Application.Services;
using HarborAdmin.Modules.Secrets.Contracts.Dtos;
using HarborAdmin.Modules.Secrets.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Secrets.Controllers;

/// <summary>
/// 通用 Secret 管理 API。
/// </summary>
[ApiController]
[Route("api/admin/secrets")]
public sealed class SecretsController(SecretService secretService) : ControllerBase
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<SecretDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await secretService.ListAsync(cancellationToken));

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SecretDto>> Save([FromBody] SaveSecretRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await secretService.SaveAsync(request, cancellationToken));

    /// <summary>
    /// 设置密钥启停状态。
    /// </summary>
    [HttpPut("enabled")]
    public async Task<ApiResult<SecretDto>> SetEnabled([FromBody] SetSecretEnabledRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await secretService.SetEnabledAsync(request, cancellationToken));
}

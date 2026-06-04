using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Secrets.Contracts.Dtos;
using HarborAdmin.Modules.Secrets.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.Secrets.Controllers;

/// <summary>
/// 通用 Secret 管理 API。
/// </summary>
[ApiController]
[Route("api/admin/secrets")]
public sealed class SecretsController(
    ISecretStore secretStore,
    IHarborMapper mapper) : ControllerBase
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SecretDto>>> List(CancellationToken cancellationToken) =>
        Ok((await secretStore.ListAsync(cancellationToken)).Select(secret => mapper.Map<SecretDto>(secret)).ToList());

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SecretDto>> Save([FromBody] SaveSecretRequest request, CancellationToken cancellationToken)
    {
        var saved = await secretStore.SaveAsync(request.SecretRef, request.DisplayName, request.SecretValue, cancellationToken);
        return Ok(mapper.Map<SecretDto>(saved));
    }

    /// <summary>
    /// 设置密钥启停状态。
    /// </summary>
    [HttpPut("enabled")]
    public async Task<ActionResult<SecretDto>> SetEnabled([FromBody] SetSecretEnabledRequest request, CancellationToken cancellationToken)
    {
        var saved = await secretStore.SetEnabledAsync(request.SecretRef, request.Enabled, cancellationToken);
        return Ok(mapper.Map<SecretDto>(saved));
    }
}

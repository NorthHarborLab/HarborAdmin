using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Mapping;
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
public sealed class SecretsController(
    ISecretStore secretStore,
    IHarborMapper mapper) : ControllerBase
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<SecretDto>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<SecretDto> data = (await secretStore.ListAsync(cancellationToken))
            .Select(secret => mapper.Map<SecretDto>(secret))
            .ToList()
            .AsReadOnly();

        return ApiResult.Ok(data);
    }

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SecretDto>> Save([FromBody] SaveSecretRequest request, CancellationToken cancellationToken)
    {
        var saved = await secretStore.SaveAsync(request.SecretRef, request.DisplayName, request.SecretValue, cancellationToken);
        return ApiResult.Ok(mapper.Map<SecretDto>(saved));
    }

    /// <summary>
    /// 设置密钥启停状态。
    /// </summary>
    [HttpPut("enabled")]
    public async Task<ApiResult<SecretDto>> SetEnabled([FromBody] SetSecretEnabledRequest request, CancellationToken cancellationToken)
    {
        var saved = await secretStore.SetEnabledAsync(request.SecretRef, request.Enabled, cancellationToken);
        return ApiResult.Ok(mapper.Map<SecretDto>(saved));
    }
}



using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Secrets.Contracts.Dtos;
using HarborAdmin.Modules.Secrets.Contracts.Requests;

namespace HarborAdmin.Modules.Secrets.Application.Services;

/// <summary>
/// 密钥管理服务。
/// </summary>
public sealed class SecretService(ISecretStore secretStore, IHarborMapper mapper)
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    public async Task<IReadOnlyList<SecretDto>> ListAsync(CancellationToken cancellationToken)
    {
        var secrets = await secretStore.ListAsync(cancellationToken);
        return secrets.Select(secret => mapper.Map<SecretDto>(secret)).ToList();
    }

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    public async Task<SecretDto> SaveAsync(SaveSecretRequest request, CancellationToken cancellationToken)
    {
        var saved = await secretStore.SaveAsync(
            request.SecretRef.Trim(),
            request.DisplayName.Trim(),
            request.SecretValue,
            cancellationToken);
        return mapper.Map<SecretDto>(saved);
    }

    /// <summary>
    /// 设置密钥启停状态。
    /// </summary>
    public async Task<SecretDto> SetEnabledAsync(SetSecretEnabledRequest request, CancellationToken cancellationToken)
    {
        var saved = await secretStore.SetEnabledAsync(request.SecretRef.Trim(), request.Enabled, cancellationToken);
        return mapper.Map<SecretDto>(saved);
    }
}

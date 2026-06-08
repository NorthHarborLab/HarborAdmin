using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.Protection;
using HarborAdmin.BuildingBlocks.Secrets.References;
using HarborAdmin.Modules.Secrets.Application.Abstractions;
using HarborAdmin.Modules.Secrets.Contracts.Secret.Dto;
using HarborAdmin.Modules.Secrets.Contracts.Secret.Request;

namespace HarborAdmin.Modules.Secrets.Application.Services;

/// <summary>
/// 密钥管理服务。
/// </summary>
public sealed class SecretService(ISecretsRepository repository, ISecretProtector protector, IHarborMapper mapper)
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    public async Task<IReadOnlyList<SecretDto>> ListAsync(CancellationToken cancellationToken)
    {
        var secrets = await repository.ListOrderedByRefAsync(cancellationToken);
        return secrets.Select(secret => mapper.Map<SecretDto>(secret)).ToList();
    }

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    public async Task<SecretDto> SaveAsync(SaveSecretRequest request, CancellationToken cancellationToken)
    {
        var normalizedRef = NormalizeRef(request.SecretRef);
        var normalizedDisplayName = NormalizeDisplayName(request.DisplayName);
        if (string.IsNullOrEmpty(request.SecretValue))
        {
            throw new ArgumentException("SecretValue is required.", nameof(request));
        }

        // HTTP 管理端只接收明文，写入仓储前必须先保护，避免明文落库。
        var saved = await repository.SaveRotationAsync(
            normalizedRef,
            normalizedDisplayName,
            protector.Protect(request.SecretValue),
            cancellationToken);
        return mapper.Map<SecretDto>(saved);
    }

    /// <summary>
    /// 设置密钥启停状态。
    /// </summary>
    public async Task<SecretDto> SetEnabledAsync(SetSecretEnabledRequest request, CancellationToken cancellationToken)
    {
        var normalizedRef = NormalizeRef(request.SecretRef);
        var entity = await repository.GetByRefAsync(normalizedRef, cancellationToken)
                     ?? throw new KeyNotFoundException($"Secret '{normalizedRef}' was not found.");
        entity.Enabled = request.Enabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateSecretAsync(entity, cancellationToken);
        return mapper.Map<SecretDto>(entity);
    }

    /// <summary>
    /// 规范化 Secret 引用并校验可用于配置标记。
    /// </summary>
    private static string NormalizeRef(string value)
    {
        if (!SecretReferenceParser.IsValidRef(value))
        {
            throw new ArgumentException("SecretRef can only contain letters, numbers, '.', '_', ':' or '-'.", nameof(value));
        }

        return value.Trim();
    }

    /// <summary>
    /// 规范化 Secret 显示名称。
    /// </summary>
    private static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("displayName is required.", nameof(value));
        }

        return value.Trim();
    }
}

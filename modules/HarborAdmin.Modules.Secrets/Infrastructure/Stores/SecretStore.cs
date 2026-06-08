using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Secrets.Protection;
using HarborAdmin.BuildingBlocks.Secrets.References;
using HarborAdmin.Modules.Secrets.Application.Abstractions;
using HarborAdmin.Modules.Secrets.Domain.Entities;

namespace HarborAdmin.Modules.Secrets.Infrastructure.Stores;

/// <summary>
/// FreeSql 通用密钥存储。
/// </summary>
public sealed class SecretStore(ISecretsRepository repository, ISecretProtector protector) : ISecretStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SecretDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var secrets = await repository.ListOrderedByRefAsync(cancellationToken);
        return secrets.Select(ToDescriptor).ToList();
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor?> GetAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return null;
        }

        var secret = await repository.GetByRefAsync(secretRef.Trim(), cancellationToken);
        return secret is null ? null : ToDescriptor(secret);
    }

    /// <inheritdoc />
    public async Task<SecretVersionDescriptor?> GetVersionAsync(string secretRef, int version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretRef) || version <= 0)
        {
            return null;
        }

        var entity = await repository.GetVersionByRefAndVersionAsync(secretRef.Trim(), version, cancellationToken);
        return entity is null ? null : new SecretVersionDescriptor(entity.Id, entity.SecretRef, entity.Version, entity.CreatedAt);
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor> SaveAsync(string secretRef, string displayName, string secretValue, CancellationToken cancellationToken = default)
    {
        var normalizedRef = NormalizeRef(secretRef);
        var normalizedDisplayName = NormalizeDisplayName(displayName);
        if (string.IsNullOrEmpty(secretValue))
        {
            throw new ArgumentException("secretValue is required.", nameof(secretValue));
        }

        var saved = await repository.SaveRotationAsync(
            normalizedRef,
            normalizedDisplayName,
            protector.Protect(secretValue),
            cancellationToken);
        return ToDescriptor(saved);
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor> SaveIfChangedAsync(string secretRef, string displayName, string secretValue, CancellationToken cancellationToken = default)
    {
        var normalizedRef = NormalizeRef(secretRef);
        var existing = await repository.GetByRefAsync(normalizedRef, cancellationToken);
        if (existing is null)
        {
            // 首次保存仍走统一轮换路径，保证当前表和版本表同时初始化。
            return await SaveAsync(normalizedRef, displayName, secretValue, cancellationToken);
        }

        var current = await ResolveAsync(normalizedRef, existing.Version, cancellationToken);
        if (!string.Equals(current, secretValue, StringComparison.Ordinal))
        {
            // 明文变化才生成新版本；这让调用方可以幂等地同步 Secret。
            return await SaveAsync(normalizedRef, displayName, secretValue, cancellationToken);
        }

        // 明文未变化时只刷新元数据并重新启用，不写入新的历史版本。
        existing.DisplayName = NormalizeDisplayName(displayName);
        existing.Enabled = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateSecretAsync(existing, cancellationToken);
        return ToDescriptor(existing);
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor> SetEnabledAsync(string secretRef, bool enabled, CancellationToken cancellationToken = default)
    {
        var normalizedRef = NormalizeRef(secretRef);
        var entity = await repository.GetByRefAsync(normalizedRef, cancellationToken)
                     ?? throw new KeyNotFoundException($"Secret '{normalizedRef}' was not found.");
        entity.Enabled = enabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateSecretAsync(entity, cancellationToken);
        return ToDescriptor(entity);
    }

    /// <inheritdoc />
    public Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default) =>
        ResolveAsync(secretRef, null, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string secretRef, int? version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return null;
        }

        var normalizedRef = secretRef.Trim();
        var secret = await repository.GetByRefAsync(normalizedRef, cancellationToken);
        if (secret is not { Enabled: true })
        {
            // 禁用状态对解析方表现为不可用，避免调用方继续拿到旧明文。
            return null;
        }

        var targetVersion = version is > 0 ? version.Value : secret.Version;
        var versionEntity = await repository.GetVersionByRefAndVersionAsync(normalizedRef, targetVersion, cancellationToken);
        var cipherText = versionEntity?.CipherText;
        if (string.IsNullOrWhiteSpace(cipherText) && targetVersion == secret.Version)
        {
            // 兼容当前表已有密文但版本表缺失的历史数据，当前版本仍可解析。
            cipherText = secret.CipherText;
        }

        return string.IsNullOrWhiteSpace(cipherText) ? null : protector.Unprotect(cipherText);
    }

    /// <summary>
    /// 转换为不含明文的 Secret 描述。
    /// </summary>
    private static SecretDescriptor ToDescriptor(HarborSecret secret) =>
        new(secret.Id, secret.SecretRef, secret.DisplayName, secret.Version, secret.Enabled, !string.IsNullOrWhiteSpace(secret.CipherText),
            secret.CreatedAt, secret.UpdatedAt ?? secret.CreatedAt);

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

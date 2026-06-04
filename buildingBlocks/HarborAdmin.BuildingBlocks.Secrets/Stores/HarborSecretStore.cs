using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Secrets.Domain;
using HarborAdmin.BuildingBlocks.Secrets.Protection;
using HarborAdmin.BuildingBlocks.Secrets.References;

namespace HarborAdmin.BuildingBlocks.Secrets.Stores;

/// <summary>
/// FreeSql 通用密钥存储。
/// </summary>
public sealed class HarborSecretStore(
    HarborFreeSqlCloud cloud,
    DbEntityRegistry entityRegistry,
    UnitOfWorkManagerCloud unitOfWorkManager,
    ISecretProtector protector) : ISecretStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SecretDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var fsql = GetFreeSql();
        var secrets = await fsql.Select<HarborSecret>()
            .OrderBy(secret => secret.SecretRef)
            .ToListAsync(cancellationToken);
        return secrets.Select(ToDescriptor).ToList();
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor?> GetAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return null;
        }

        var secret = await GetFreeSql().Select<HarborSecret>()
            .Where(item => item.SecretRef == secretRef.Trim())
            .FirstAsync(cancellationToken);
        return secret is null ? null : ToDescriptor(secret);
    }

    /// <inheritdoc />
    public async Task<SecretVersionDescriptor?> GetVersionAsync(string secretRef, int version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretRef) || version <= 0)
        {
            return null;
        }

        var entity = await GetFreeSql().Select<HarborSecretVersion>()
            .Where(item => item.SecretRef == secretRef.Trim() && item.Version == version)
            .FirstAsync(cancellationToken);
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

        var dbKey = entityRegistry.GetDbKey<HarborSecret>();
        using var uow = unitOfWorkManager.Begin(dbKey);
        var fsql = uow.Orm;
        var now = DateTimeOffset.UtcNow;
        var existing = await fsql.Select<HarborSecret>()
            .Where(item => item.SecretRef == normalizedRef)
            .FirstAsync(cancellationToken);
        var latestVersion = await fsql.Select<HarborSecretVersion>()
            .Where(item => item.SecretRef == normalizedRef)
            .MaxAsync(item => item.Version, cancellationToken);
        var nextVersion = Math.Max(existing?.Version ?? 0, latestVersion) + 1;
        var cipherText = protector.Protect(secretValue);

        var version = new HarborSecretVersion
        {
            SecretRef = normalizedRef,
            Version = nextVersion,
            CipherText = cipherText,
            CreatedAt = now
        };
        await fsql.Insert(version).ExecuteAffrowsAsync(cancellationToken);

        HarborSecret saved;
        if (existing is null)
        {
            saved = new HarborSecret
            {
                SecretRef = normalizedRef,
                DisplayName = normalizedDisplayName,
                CipherText = cipherText,
                Version = nextVersion,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            var inserted = await fsql.Insert(saved).ExecuteInsertedAsync(cancellationToken);
            saved.Id = inserted.FirstOrDefault()?.Id ?? saved.Id;
        }
        else
        {
            existing.DisplayName = normalizedDisplayName;
            existing.CipherText = cipherText;
            existing.Version = nextVersion;
            existing.Enabled = true;
            existing.UpdatedAt = now;
            await fsql.Update<HarborSecret>().SetSource(existing).ExecuteAffrowsAsync(cancellationToken);
            saved = existing;
        }

        uow.Commit();
        return ToDescriptor(saved);
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor> SaveIfChangedAsync(string secretRef, string displayName, string secretValue, CancellationToken cancellationToken = default)
    {
        var normalizedRef = NormalizeRef(secretRef);
        var existing = await GetSecretEntityAsync(normalizedRef, cancellationToken);
        if (existing is null)
        {
            return await SaveAsync(normalizedRef, displayName, secretValue, cancellationToken);
        }

        var current = await ResolveAsync(normalizedRef, existing.Version, cancellationToken);
        if (!string.Equals(current, secretValue, StringComparison.Ordinal))
        {
            return await SaveAsync(normalizedRef, displayName, secretValue, cancellationToken);
        }

        existing.DisplayName = NormalizeDisplayName(displayName);
        existing.Enabled = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await GetFreeSql().Update<HarborSecret>().SetSource(existing).ExecuteAffrowsAsync(cancellationToken);
        return ToDescriptor(existing);
    }

    /// <inheritdoc />
    public async Task<SecretDescriptor> SetEnabledAsync(string secretRef, bool enabled, CancellationToken cancellationToken = default)
    {
        var normalizedRef = NormalizeRef(secretRef);
        var entity = await GetSecretEntityAsync(normalizedRef, cancellationToken)
                     ?? throw new KeyNotFoundException($"Secret '{normalizedRef}' was not found.");
        entity.Enabled = enabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await GetFreeSql().Update<HarborSecret>().SetSource(entity).ExecuteAffrowsAsync(cancellationToken);
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
        var fsql = GetFreeSql();
        var secret = await fsql.Select<HarborSecret>()
            .Where(item => item.SecretRef == normalizedRef)
            .FirstAsync(cancellationToken);
        if (secret is not { Enabled: true })
        {
            return null;
        }

        var targetVersion = version is > 0 ? version.Value : secret.Version;
        var versionEntity = await fsql.Select<HarborSecretVersion>()
            .Where(item => item.SecretRef == normalizedRef && item.Version == targetVersion)
            .FirstAsync(cancellationToken);
        var cipherText = versionEntity?.CipherText;
        if (string.IsNullOrWhiteSpace(cipherText) && targetVersion == secret.Version)
        {
            cipherText = secret.CipherText;
        }

        return string.IsNullOrWhiteSpace(cipherText) ? null : protector.Unprotect(cipherText);
    }

    private IFreeSql GetFreeSql() => cloud.Use(entityRegistry.GetDbKey<HarborSecret>());

    private async Task<HarborSecret?> GetSecretEntityAsync(string secretRef, CancellationToken cancellationToken) =>
        await GetFreeSql().Select<HarborSecret>()
            .Where(item => item.SecretRef == secretRef)
            .FirstAsync(cancellationToken);

    private static SecretDescriptor ToDescriptor(HarborSecret secret) =>
        new(secret.Id, secret.SecretRef, secret.DisplayName, secret.Version, secret.Enabled, !string.IsNullOrWhiteSpace(secret.CipherText),
            secret.CreatedAt, secret.UpdatedAt);

    private static string NormalizeRef(string value)
    {
        if (!SecretReferenceParser.IsValidRef(value))
        {
            throw new ArgumentException("SecretRef can only contain letters, numbers, '.', '_', ':' or '-'.", nameof(value));
        }

        return value.Trim();
    }

    private static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("displayName is required.", nameof(value));
        }

        return value.Trim();
    }
}
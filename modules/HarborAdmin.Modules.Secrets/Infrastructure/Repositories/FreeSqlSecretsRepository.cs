using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.Secrets.Application.Abstractions;
using HarborAdmin.Modules.Secrets.Domain.Entities;
using HarborAdmin.Modules.Secrets.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Secrets.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 Secrets 仓储实现。
/// </summary>
public sealed class FreeSqlSecretsRepository(ISecretsDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<ISecretsDbContext>(db, unitOfWorkManager), ISecretsRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<HarborSecret>> ListOrderedByRefAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<HarborSecret>()
            .OrderBy(secret => secret.SecretRef)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<HarborSecret?> GetByRefAsync(string secretRef, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<HarborSecret>()
            .Where(item => item.SecretRef == secretRef)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<HarborSecretVersion?> GetVersionByRefAndVersionAsync(string secretRef, int version, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<HarborSecretVersion>()
            .Where(item => item.SecretRef == secretRef && item.Version == version)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> GetMaxVersionAsync(string secretRef, CancellationToken cancellationToken = default) =>
        FreeSql.Select<HarborSecretVersion>()
            .Where(item => item.SecretRef == secretRef)
            .MaxAsync(item => item.Version, cancellationToken);

    /// <inheritdoc />
    public async Task<HarborSecret> InsertSecretAsync(HarborSecret secret, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(secret).ExecuteInsertedAsync(cancellationToken);
        var saved = inserted.FirstOrDefault();
        if (saved is not null)
        {
            secret.Id = saved.Id;
        }

        return secret;
    }

    /// <inheritdoc />
    public Task UpdateSecretAsync(HarborSecret secret, CancellationToken cancellationToken = default) =>
        FreeSql.Update<HarborSecret>().SetSource(secret).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public Task InsertVersionAsync(HarborSecretVersion version, CancellationToken cancellationToken = default) =>
        FreeSql.Insert(version).ExecuteAffrowsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<HarborSecret> SaveRotationAsync(string secretRef, string displayName, string cipherText, CancellationToken cancellationToken = default)
    {
        return await ExecuteInUnitOfWorkAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var existing = await FreeSql.Select<HarborSecret>()
                .Where(item => item.SecretRef == secretRef)
                .FirstAsync(ct);
            var latestVersion = await FreeSql.Select<HarborSecretVersion>()
                .Where(item => item.SecretRef == secretRef)
                .MaxAsync(item => item.Version, ct);
            // 当前表和历史表都可能是版本来源，取两者最大值再递增可兼容历史数据修复场景。
            var nextVersion = Math.Max(existing?.Version ?? 0, latestVersion) + 1;

            // 先写历史版本，再写当前指针；两步在同一个 UoW 内提交，避免只留下半次轮换。
            await FreeSql.Insert(new HarborSecretVersion
            {
                SecretRef = secretRef,
                Version = nextVersion,
                CipherText = cipherText,
                CreatedAt = now
            }).ExecuteAffrowsAsync(ct);

            HarborSecret saved;
            if (existing is null)
            {
                saved = new HarborSecret
                {
                    SecretRef = secretRef,
                    DisplayName = displayName,
                    CipherText = cipherText,
                    Version = nextVersion,
                    Enabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                var inserted = await FreeSql.Insert(saved).ExecuteInsertedAsync(ct);
                saved.Id = inserted.FirstOrDefault()?.Id ?? saved.Id;
            }
            else
            {
                existing.DisplayName = displayName;
                existing.CipherText = cipherText;
                existing.Version = nextVersion;
                existing.Enabled = true;
                existing.UpdatedAt = now;
                await FreeSql.Update<HarborSecret>().SetSource(existing).ExecuteAffrowsAsync(ct);
                saved = existing;
            }

            return saved;
        }, cancellationToken);
    }
}

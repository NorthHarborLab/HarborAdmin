using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 运行时状态 FreeSql 仓储。
/// </summary>
public sealed class AdminRuntimeStateRepository(IAdminDbContext db, IHarborCache cache, IHarborCacheInvalidator cacheInvalidator)
    : FreeSqlModuleRepository<IAdminDbContext>(db), IAdminRuntimeStateRepository
{
    private const string SessionVersionKey = "global";

    /// <inheritdoc />
    public async Task<long> GetSessionVersionValueAsync(CancellationToken cancellationToken = default)
    {
        var version = await FreeSql.Select<AdminSessionVersion>()
            .Where(item => item.VersionKey == SessionVersionKey)
            .ToOneAsync(cancellationToken);
        if (version is not null)
        {
            return version.Version;
        }

        await FreeSql.Insert(new AdminSessionVersion
        {
            VersionKey = SessionVersionKey,
            Version = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
        }).ExecuteAffrowsAsync(cancellationToken);
        return 1;
    }

    /// <inheritdoc />
    public async Task BumpSessionVersionAsync(CancellationToken cancellationToken = default)
    {
        var version = await FreeSql.Select<AdminSessionVersion>()
            .Where(item => item.VersionKey == SessionVersionKey)
            .ToOneAsync(cancellationToken);
        if (version is null)
        {
            await FreeSql.Insert(new AdminSessionVersion
            {
                VersionKey = SessionVersionKey,
                Version = 2,
                UpdatedAt = DateTimeOffset.UtcNow,
            }).ExecuteAffrowsAsync(cancellationToken);
        }
        else
        {
            version.Version++;
            version.UpdatedAt = DateTimeOffset.UtcNow;
            await FreeSql.Update<AdminSessionVersion>().SetSource(version).ExecuteAffrowsAsync(cancellationToken);
        }

        await cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.AllUsersTag, cancellationToken);
        await cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.AllRolesTag, cancellationToken);
        await cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.RuntimeTag, cancellationToken);
        await cache.Get<SessionVersionCacheModel>()
            .Where(item => item.VersionKey == AdminAccessCacheKeys.SessionVersionId)
            .RemoveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidateDictionaryRuntimeAsync(CancellationToken cancellationToken = default)
    {
        await cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.RuntimeTag, cancellationToken);
        await BumpSessionVersionAsync(cancellationToken);
    }
}

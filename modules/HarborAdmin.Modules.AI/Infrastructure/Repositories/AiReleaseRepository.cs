using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 配置发布 FreeSql 仓储。
/// </summary>
public sealed class AiReleaseRepository(IAiDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlModuleRepository<IAiDbContext>(db), IAiReleaseRepository
{
    /// <inheritdoc />
    public async Task<AiConfigRelease> InsertAndActivateReleaseAsync(AiConfigRelease release, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbContext.DbKey);
        using (DbContext.Bind(DbContext.DbKey, uow.Orm))
        {
            await InsertReleaseCoreAsync(release, cancellationToken);
            await ActivateReleaseCoreAsync(release.Id, cancellationToken);
            release.Active = true;
        }

        uow.Commit();
        return release;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiConfigRelease>> ListReleasesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().OrderByDescending(release => release.Version).Limit(100).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConfigRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().Where(release => release.Active).OrderByDescending(release => release.Version).FirstAsync(cancellationToken)
        ?? await FreeSql.Select<AiConfigRelease>().OrderByDescending(release => release.Version).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConfigRelease?> GetReleaseAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().Where(release => release.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConfigRelease?> GetReleaseByVersionAsync(int version, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().Where(release => release.Version == version).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ActivateReleaseAsync(long releaseId, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbContext.DbKey);
        using (DbContext.Bind(DbContext.DbKey, uow.Orm))
        {
            await ActivateReleaseCoreAsync(releaseId, cancellationToken);
        }

        uow.Commit();
    }

    /// <summary>
    /// 插入发布记录。
    /// </summary>
    private async Task InsertReleaseCoreAsync(AiConfigRelease release, CancellationToken cancellationToken)
    {
        var inserted = await FreeSql.Insert(release).ExecuteInsertedAsync(cancellationToken);
        release.Id = inserted.First().Id;
    }

    /// <summary>
    /// 激活指定发布记录。
    /// </summary>
    private async Task ActivateReleaseCoreAsync(long releaseId, CancellationToken cancellationToken)
    {
        await FreeSql.Update<AiConfigRelease>().Set(release => release.Active, false).Where(release => release.Active).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Update<AiConfigRelease>().Set(release => release.Active, true).Where(release => release.Id == releaseId).ExecuteAffrowsAsync(cancellationToken);
    }
}

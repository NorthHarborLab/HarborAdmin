using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI 发布仓储实现 partial。
/// </summary>
public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<AiConfigRelease> InsertReleaseAsync(AiConfigRelease release, CancellationToken cancellationToken = default)
    {
        var inserted = await FreeSql.Insert(release).ExecuteInsertedAsync(cancellationToken);
        release.Id = inserted.First().Id;
        return release;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiConfigRelease>> ListReleasesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().OrderByDescending(r => r.Version).Limit(100).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConfigRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().Where(r => r.Active).OrderByDescending(r => r.Version).FirstAsync(cancellationToken)
        ?? await FreeSql.Select<AiConfigRelease>().OrderByDescending(r => r.Version).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConfigRelease?> GetReleaseAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().Where(r => r.Id == id).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiConfigRelease?> GetReleaseByVersionAsync(int version, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiConfigRelease>().Where(r => r.Version == version).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ActivateReleaseAsync(long releaseId, CancellationToken cancellationToken = default)
    {
        await FreeSql.Update<AiConfigRelease>().Set(r => r.Active, false).Where(r => r.Active).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Update<AiConfigRelease>().Set(r => r.Active, true).Where(r => r.Id == releaseId).ExecuteAffrowsAsync(cancellationToken);
    }
}

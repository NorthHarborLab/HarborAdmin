using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI 业务仓储实现 partial。
/// </summary>
public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AiBusiness>> ListBusinessesAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiBusiness>().IncludeMany(b => b.Routes).OrderBy(b => b.BusinessKey).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiBusiness?> GetBusinessByKeyAsync(string businessKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiBusiness>().Where(b => b.BusinessKey == businessKey).IncludeMany(b => b.Routes).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiBusiness?> GetBusinessAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiBusiness>().Where(b => b.Id == id).IncludeMany(b => b.Routes).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiBusiness> SaveBusinessAsync(AiBusiness business, IReadOnlyList<AiBusinessProviderRoute> routes, CancellationToken cancellationToken = default)
    {
        if (business.Id == 0)
        {
            var inserted = await FreeSql.Insert(business).ExecuteInsertedAsync(cancellationToken);
            business.Id = inserted.First().Id;
        }
        else
        {
            await FreeSql.Update<AiBusiness>().SetSource(business).ExecuteAffrowsAsync(cancellationToken);
            await FreeSql.Delete<AiBusinessProviderRoute>().Where(r => r.BusinessId == business.Id).ExecuteAffrowsAsync(cancellationToken);
        }

        foreach (var route in routes)
        {
            route.BusinessId = business.Id;
            await FreeSql.Insert(route).ExecuteAffrowsAsync(cancellationToken);
        }

        business.Routes = routes.OrderBy(r => r.Priority).ToList();
        return business;
    }

    /// <inheritdoc />
    public async Task DeleteBusinessAsync(long id, CancellationToken cancellationToken = default)
    {
        await FreeSql.Delete<AiBusinessProviderRoute>().Where(r => r.BusinessId == id).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AiBusiness>().Where(b => b.Id == id).ExecuteAffrowsAsync(cancellationToken);
    }
}

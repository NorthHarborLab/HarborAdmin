using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI 供应商仓储实现 partial。
/// </summary>
public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AiProvider>> ListProvidersAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProvider>().IncludeMany(p => p.Models).OrderBy(p => p.ProviderKey).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiProvider?> GetProviderByKeyAsync(string providerKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProvider>().Where(p => p.ProviderKey == providerKey).IncludeMany(p => p.Models).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiProvider?> GetProviderAsync(long id, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProvider>().Where(p => p.Id == id).IncludeMany(p => p.Models).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiProvider> SaveProviderAsync(AiProvider provider, IReadOnlyList<AiProviderModel> models, CancellationToken cancellationToken = default)
    {
        if (provider.Id == 0)
        {
            var inserted = await FreeSql.Insert(provider).ExecuteInsertedAsync(cancellationToken);
            provider.Id = inserted.First().Id;
        }
        else
        {
            await FreeSql.Update<AiProvider>().SetSource(provider).ExecuteAffrowsAsync(cancellationToken);
            await FreeSql.Delete<AiProviderModel>().Where(m => m.ProviderId == provider.Id).ExecuteAffrowsAsync(cancellationToken);
        }

        foreach (var model in models)
        {
            model.ProviderId = provider.Id;
            await FreeSql.Insert(model).ExecuteAffrowsAsync(cancellationToken);
        }

        provider.Models = models.OrderBy(m => m.SortOrder).ThenBy(m => m.ModelName).ToList();
        return provider;
    }

    /// <inheritdoc />
    public async Task DeleteProviderAsync(long id, CancellationToken cancellationToken = default)
    {
        await FreeSql.Delete<AiProviderModel>().Where(m => m.ProviderId == id).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AiProviderQuota>().Where(q => q.ProviderId == id).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AiProvider>().Where(p => p.Id == id).ExecuteAffrowsAsync(cancellationToken);
    }
}

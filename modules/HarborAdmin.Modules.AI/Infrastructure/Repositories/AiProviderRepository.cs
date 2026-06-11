using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 供应商实体 CRUD 仓储。
/// </summary>
public sealed class AiProviderRepository(IAiDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlEntityRepository<AiProvider, IAiDbContext>(db, entityRegistry), IAiProviderRepository
{
    /// <inheritdoc />
    protected override ISelect<AiProvider> BuildListQuery(ISelect<AiProvider> query) =>
        query.IncludeMany(provider => provider.Models).OrderBy(provider => provider.ProviderKey);

    /// <inheritdoc />
    protected override ISelect<AiProvider> BuildDetailQuery(ISelect<AiProvider> query) =>
        query.IncludeMany(provider => provider.Models);

    /// <inheritdoc />
    public async Task<AiProvider?> GetProviderByKeyAsync(string providerKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProvider>()
            .Where(provider => provider.ProviderKey == providerKey)
            .IncludeMany(provider => provider.Models)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public override async Task<AiProvider> InsertAsync(AiProvider entity, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbKey);
        using (DbContext.Bind(DbKey, uow.Orm))
        {
            await base.InsertAsync(entity, cancellationToken);
            await SaveModelsAsync(entity, cancellationToken);
        }

        uow.Commit();
        return entity;
    }

    /// <inheritdoc />
    public override async Task<AiProvider> UpdateAsync(AiProvider entity, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbKey);
        using (DbContext.Bind(DbKey, uow.Orm))
        {
            await base.UpdateAsync(entity, cancellationToken);
            await FreeSql.Delete<AiProviderModel>().Where(model => model.ProviderId == entity.Id).ExecuteAffrowsAsync(cancellationToken);
            await SaveModelsAsync(entity, cancellationToken);
        }

        uow.Commit();
        return entity;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbKey);
        using (DbContext.Bind(DbKey, uow.Orm))
        {
            await FreeSql.Delete<AiProviderModel>().Where(model => model.ProviderId == id).ExecuteAffrowsAsync(cancellationToken);
            await FreeSql.Delete<AiProviderQuota>().Where(quota => quota.ProviderId == id).ExecuteAffrowsAsync(cancellationToken);
            await base.DeleteAsync(id, cancellationToken);
        }

        uow.Commit();
    }

    /// <summary>
    /// 保存供应商模型列表。
    /// </summary>
    private async Task SaveModelsAsync(AiProvider entity, CancellationToken cancellationToken)
    {
        foreach (var model in entity.Models.OrderBy(model => model.SortOrder).ThenBy(model => model.ModelName))
        {
            model.ProviderId = entity.Id;
            await FreeSql.Insert(model).ExecuteAffrowsAsync(cancellationToken);
        }

        entity.Models = entity.Models.OrderBy(model => model.SortOrder).ThenBy(model => model.ModelName).ToList();
    }
}

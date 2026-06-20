using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 供应商实体 CRUD 仓储。
/// </summary>
public sealed class AiProviderRepository(IAiDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AiProvider, IAiDbContext>(db, entityRegistry, unitOfWorkManager), IAiProviderRepository
{
    /// <inheritdoc />
    public async Task<AiProvider?> GetProviderByKeyAsync(string providerKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiProvider>()
            .Where(provider => provider.ProviderKey == providerKey)
            .IncludeMany(provider => provider.Models)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public override async Task<AiProvider> InsertAsync(AiProvider entity, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await base.InsertAsync(entity, ct);
            await SaveModelsAsync(entity, ct);
        }, cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public override async Task<AiProvider> UpdateAsync(AiProvider entity, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await base.UpdateAsync(entity, ct);
            await FreeSql.Delete<AiProviderModel>().Where(model => model.ProviderId == entity.Id).ExecuteAffrowsAsync(ct);
            await SaveModelsAsync(entity, ct);
        }, cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await FreeSql.Delete<AiProviderModel>().Where(model => model.ProviderId == id).ExecuteAffrowsAsync(ct);
            await FreeSql.Delete<AiProviderQuota>().Where(quota => quota.ProviderId == id).ExecuteAffrowsAsync(ct);
            await base.DeleteAsync(id, ct);
        }, cancellationToken);
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

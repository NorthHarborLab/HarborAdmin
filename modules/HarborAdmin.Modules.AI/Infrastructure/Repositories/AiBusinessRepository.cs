using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 业务实体 CRUD 仓储。
/// </summary>
public sealed class AiBusinessRepository(IAiDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AiBusiness, IAiDbContext>(db, entityRegistry, unitOfWorkManager), IAiBusinessRepository
{
    /// <inheritdoc />
    public async Task<AiBusiness?> GetBusinessByKeyAsync(string businessKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiBusiness>()
            .Where(business => business.BusinessKey == businessKey)
            .IncludeMany(business => business.Routes)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> BusinessKeyExistsAsync(string businessKey, long? excludeId, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AiBusiness>().Where(entity => entity.BusinessKey == businessKey);
        if (excludeId.HasValue)
        {
            query = query.Where(entity => entity.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<AiBusiness> InsertAsync(AiBusiness entity, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await base.InsertAsync(entity, ct);
            await SaveRoutesAsync(entity, ct);
        }, cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public override async Task<AiBusiness> UpdateAsync(AiBusiness entity, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await base.UpdateAsync(entity, ct);
            await FreeSql.Delete<AiBusinessProviderRoute>().Where(route => route.BusinessId == entity.Id).ExecuteAffrowsAsync(ct);
            await SaveRoutesAsync(entity, ct);
        }, cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await FreeSql.Delete<AiBusinessProviderRoute>().Where(route => route.BusinessId == id).ExecuteAffrowsAsync(ct);
            await base.DeleteAsync(id, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// 保存业务路由列表。
    /// </summary>
    private async Task SaveRoutesAsync(AiBusiness entity, CancellationToken cancellationToken)
    {
        foreach (var route in entity.Routes.OrderBy(route => route.Priority))
        {
            route.BusinessId = entity.Id;
            await FreeSql.Insert(route).ExecuteAffrowsAsync(cancellationToken);
        }

        entity.Routes = entity.Routes.OrderBy(route => route.Priority).ToList();
    }
}

using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// AI 业务实体 CRUD 仓储。
/// </summary>
public sealed class AiBusinessRepository(IAiDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlEntityRepository<AiBusiness, IAiDbContext>(db, entityRegistry), IAiBusinessRepository
{
    /// <inheritdoc />
    protected override ISelect<AiBusiness> BuildListQuery(ISelect<AiBusiness> query) =>
        query.IncludeMany(business => business.Routes).OrderBy(business => business.BusinessKey);

    /// <inheritdoc />
    protected override ISelect<AiBusiness> BuildDetailQuery(ISelect<AiBusiness> query) =>
        query.IncludeMany(business => business.Routes);

    /// <inheritdoc />
    public async Task<AiBusiness?> GetBusinessByKeyAsync(string businessKey, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiBusiness>()
            .Where(business => business.BusinessKey == businessKey)
            .IncludeMany(business => business.Routes)
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public override async Task<AiBusiness> InsertAsync(AiBusiness entity, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbKey);
        using (DbContext.Bind(DbKey, uow.Orm))
        {
            await base.InsertAsync(entity, cancellationToken);
            await SaveRoutesAsync(entity, cancellationToken);
        }

        uow.Commit();
        return entity;
    }

    /// <inheritdoc />
    public override async Task<AiBusiness> UpdateAsync(AiBusiness entity, CancellationToken cancellationToken = default)
    {
        using var uow = unitOfWorkManager.Begin(DbKey);
        using (DbContext.Bind(DbKey, uow.Orm))
        {
            await base.UpdateAsync(entity, cancellationToken);
            await FreeSql.Delete<AiBusinessProviderRoute>().Where(route => route.BusinessId == entity.Id).ExecuteAffrowsAsync(cancellationToken);
            await SaveRoutesAsync(entity, cancellationToken);
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
            await FreeSql.Delete<AiBusinessProviderRoute>().Where(route => route.BusinessId == id).ExecuteAffrowsAsync(cancellationToken);
            await base.DeleteAsync(id, cancellationToken);
        }

        uow.Commit();
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
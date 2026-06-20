using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.BuildingBlocks.Data.Repositories;

/// <summary>
/// FreeSql 实体 CRUD 仓储基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDbContext">模块数据库上下文类型</typeparam>
public abstract class FreeSqlCrudRepository<TEntity, TDbContext>(TDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlQueryRepository<TEntity, TDbContext>(db, entityRegistry, unitOfWorkManager), IHarborCrudRepository<TEntity>
    where TEntity : EntityBase, new()
    where TDbContext : IHarborModuleDbContext
{
    /// <inheritdoc />
    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await BeforeInsertAsync(entity, cancellationToken);
        var inserted = await FreeSql.Insert(entity).ExecuteInsertedAsync(cancellationToken);
        var saved = inserted.FirstOrDefault();
        if (saved is not null)
        {
            entity.Id = saved.Id;
        }

        await AfterInsertAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await BeforeUpdateAsync(entity, cancellationToken);
        await FreeSql.Update<TEntity>().SetSource(entity).ExecuteAffrowsAsync(cancellationToken);
        await AfterUpdateAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await FreeSql.Delete<TEntity>().Where(entity => entity.Id == id).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is not ISoftDelete softDelete)
        {
            throw new ValidationDomainException($"Entity '{typeof(TEntity).FullName}' does not support soft delete.");
        }

        softDelete.IsDeleted = true;
        softDelete.DeletedAt = DateTimeOffset.UtcNow;
        await UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 插入前处理
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected virtual Task BeforeInsertAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 插入后处理
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected virtual Task AfterInsertAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 更新前处理
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected virtual Task BeforeUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 更新后处理
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected virtual Task AfterUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;
}

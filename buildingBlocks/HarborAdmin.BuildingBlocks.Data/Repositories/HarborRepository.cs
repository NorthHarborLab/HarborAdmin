using System.Data;
using FreeSql;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.BuildingBlocks.Data.Repositories;

/// <summary>
/// FreeSql 仓储基类
/// </summary>
/// <typeparam name="TDbContext">模块数据库上下文类型</typeparam>
public abstract class HarborRepository<TDbContext> : IHarborRepository 
    where TDbContext : IHarborModuleDbContext
{
    private readonly TDbContext _db;
    private readonly UnitOfWorkManagerCloud _unitOfWorkManager;
    private readonly DbEntityRegistry? _entityRegistry;

    /// <summary>
    /// 初始化 FreeSql 仓储
    /// </summary>
    /// <param name="db">模块数据库上下文</param>
    /// <param name="unitOfWorkManager">多库工作单元管理器</param>
    /// <param name="entityRegistry">实体数据库映射注册表</param>
    protected HarborRepository(TDbContext db, UnitOfWorkManagerCloud unitOfWorkManager, DbEntityRegistry? entityRegistry = null)
    {
        _db = db;
        _unitOfWorkManager = unitOfWorkManager;
        _entityRegistry = entityRegistry;
    }

    /// <summary>
    /// 当前仓储默认数据库 Key
    /// </summary>
    protected virtual string DbKey => _db.DbKey;

    /// <summary>
    /// 模块数据库上下文
    /// </summary>
    protected TDbContext DbContext => _db;

    /// <summary>
    /// 多库工作单元管理器
    /// </summary>
    protected UnitOfWorkManagerCloud UnitOfWorkManager => _unitOfWorkManager;

    /// <summary>
    /// 当前仓储默认 ORM
    /// </summary>
    protected IFreeSql FreeSql => _db.GetOrm(DbKey);

    /// <summary>
    /// 解析实体最终数据库 Key
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>数据库 Key</returns>
    protected string ResolveDbKey<TEntity>() where TEntity : class =>
        _entityRegistry is not null && _entityRegistry.TryGetDbKey(typeof(TEntity), out var dbKey) ? dbKey : _db.DbKey;

    /// <summary>
    /// 获取实体仓储
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="cascadeSave">是否启用级联保存</param>
    /// <returns>FreeSql 实体仓储</returns>
    protected IBaseRepository<TEntity> GetRepository<TEntity>(bool cascadeSave = false)
        where TEntity : class
    {
        var repository = _db.GetOrm(ResolveDbKey<TEntity>()).GetRepository<TEntity>();
        repository.DbContextOptions.EnableCascadeSave = cascadeSave;
        return repository;
    }

    /// <summary>
    /// 插入实体并回填雪花 ID
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="entity">待插入实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插入后的实体</returns>
    protected async Task<TEntity> InsertAndFillIdAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
        where TEntity : EntityBase
    {
        var inserted = await _db.GetOrm(ResolveDbKey<TEntity>()).Insert(entity).ExecuteInsertedAsync(cancellationToken);
        var saved = inserted.FirstOrDefault();
        if (saved is not null)
        {
            entity.Id = saved.Id;
        }

        return entity;
    }

    /// <summary>
    /// 在当前仓储数据库中执行工作单元。
    /// </summary>
    /// <param name="action">需要放入同一个事务的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="propagation">事务传播行为</param>
    /// <param name="isolationLevel">事务隔离级别</param>
    /// <remarks>
    /// 示例：
    /// <code>
    /// await ExecuteInUnitOfWorkAsync(async ct =>
    /// {
    ///     await base.UpdateAsync(entity, ct);
    ///     await FreeSql.Delete&lt;ChildEntity&gt;().Where(x => x.ParentId == entity.Id).ExecuteAffrowsAsync(ct);
    /// }, cancellationToken);
    /// </code>
    /// </remarks>
    protected Task ExecuteInUnitOfWorkAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        Propagation propagation = Propagation.Required,
        IsolationLevel? isolationLevel = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ExecuteInUnitOfWorkAsync<object?>(
            async token =>
            {
                await action(token);
                return null;
            },
            cancellationToken,
            propagation,
            isolationLevel);
    }

    /// <summary>
    /// 在当前仓储数据库中执行带返回值的工作单元。
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="action">需要放入同一个事务的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="propagation">事务传播行为</param>
    /// <param name="isolationLevel">事务隔离级别</param>
    /// <returns>工作单元操作返回值</returns>
    /// <remarks>
    /// 示例：
    /// <code>
    /// var saved = await ExecuteInUnitOfWorkAsync(async ct =>
    /// {
    ///     await base.InsertAsync(entity, ct);
    ///     await FreeSql.Insert(entity.Children).ExecuteAffrowsAsync(ct);
    ///     return entity;
    /// }, cancellationToken);
    /// </code>
    /// </remarks>
    protected async Task<TResult> ExecuteInUnitOfWorkAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken,
        Propagation propagation = Propagation.Required,
        IsolationLevel? isolationLevel = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var uow = _unitOfWorkManager.Begin(DbKey, propagation, isolationLevel);
        using (DbContext.Bind(DbKey, uow.Orm))
        {
            try
            {
                var result = await action(cancellationToken);
                uow.Commit();
                return result;
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }
    }
}

/// <summary>
/// FreeSql 实体仓储基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDbContext">模块数据库上下文类型</typeparam>
public abstract class HarborRepository<TEntity, TDbContext> : HarborRepository<TDbContext>, IHarborRepository<TEntity>
    where TEntity : EntityBase
    where TDbContext : IHarborModuleDbContext
{
    /// <summary>
    /// 初始化 FreeSql 实体仓储
    /// </summary>
    /// <param name="db">模块数据库上下文</param>
    /// <param name="entityRegistry">实体数据库映射注册表</param>
    /// <param name="unitOfWorkManager">多库工作单元管理器</param>
    protected HarborRepository(TDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
        : base(db, unitOfWorkManager, entityRegistry)
    {
    }

    /// <summary>
    /// 当前实体最终数据库 Key
    /// </summary>
    protected override string DbKey => ResolveDbKey<TEntity>();
}

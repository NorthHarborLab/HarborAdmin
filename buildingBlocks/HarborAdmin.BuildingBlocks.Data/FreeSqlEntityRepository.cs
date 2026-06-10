using FreeSql;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// FreeSql 实体 CRUD 仓储基类。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <typeparam name="TDbContext">模块数据库上下文类型。</typeparam>
public abstract class FreeSqlEntityRepository<TEntity, TDbContext> : IHarborCrudRepository<TEntity>
    where TEntity : EntityBase, new()
    where TDbContext : IHarborModuleDbContext
{
    private readonly TDbContext _db;
    private readonly DbEntityRegistry _entityRegistry;

    /// <summary>
    /// 初始化实体 CRUD 仓储。
    /// </summary>
    /// <param name="db">模块数据库上下文。</param>
    /// <param name="entityRegistry">实体数据库映射注册表。</param>
    protected FreeSqlEntityRepository(TDbContext db, DbEntityRegistry entityRegistry)
    {
        _db = db;
        _entityRegistry = entityRegistry;
    }

    /// <summary>
    /// 实体最终数据库 Key。
    /// </summary>
    protected string DbKey => _entityRegistry.GetDbKey<TEntity>();

    /// <summary>
    /// 模块数据库上下文。
    /// </summary>
    protected TDbContext DbContext => _db;

    /// <summary>
    /// 当前实体 ORM。
    /// </summary>
    protected IFreeSql FreeSql => _db.GetOrm(DbKey);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tableName = await ResolveListTableNameAsync(cancellationToken);
        var query = BuildListQuery(ApplyTable(FreeSql.Select<TEntity>(), tableName));
        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> PageAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var tableName = await ResolvePageTableNameAsync(request, cancellationToken);
        var query = BuildPageQuery(ApplyTable(FreeSql.Select<TEntity>(), tableName), request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Limit(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<TEntity>.From(items, total > int.MaxValue ? int.MaxValue : (int)total);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var tableName = await ResolveGetTableNameAsync(id, cancellationToken);
        return await BuildDetailQuery(ApplyTable(FreeSql.Select<TEntity>(), tableName))
            .Where(entity => entity.Id == id)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await BeforeInsertAsync(entity, cancellationToken);
        var tableName = await ResolveInsertTableNameAsync(entity, cancellationToken);
        var insert = ApplyTable(FreeSql.Insert(entity), tableName);
        var inserted = await insert.ExecuteInsertedAsync(cancellationToken);
        var saved = inserted.FirstOrDefault();
        if (saved is not null)
        {
            entity.Id = saved.Id;
        }

        await AfterInsertAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await BeforeUpdateAsync(entity, cancellationToken);
        var tableName = await ResolveUpdateTableNameAsync(entity, cancellationToken);
        await ApplyTable(FreeSql.Update<TEntity>().SetSource(entity), tableName)
            .ExecuteAffrowsAsync(cancellationToken);
        await AfterUpdateAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var tableName = await ResolveDeleteTableNameAsync(id, cancellationToken);
        await ApplyTable(FreeSql.Delete<TEntity>().Where(entity => entity.Id == id), tableName)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
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
    /// 构造列表查询。
    /// </summary>
    protected virtual ISelect<TEntity> BuildListQuery(ISelect<TEntity> query) => query;

    /// <summary>
    /// 构造分页查询。
    /// </summary>
    protected virtual ISelect<TEntity> BuildPageQuery(ISelect<TEntity> query, PageRequest request) => BuildListQuery(query);

    /// <summary>
    /// 构造详情查询。
    /// </summary>
    protected virtual ISelect<TEntity> BuildDetailQuery(ISelect<TEntity> query) => query;

    /// <summary>
    /// 解析列表查询物理表名。
    /// </summary>
    protected virtual ValueTask<string?> ResolveListTableNameAsync(CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    /// <summary>
    /// 解析分页查询物理表名。
    /// </summary>
    protected virtual ValueTask<string?> ResolvePageTableNameAsync(PageRequest request, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    /// <summary>
    /// 解析详情查询物理表名。
    /// </summary>
    protected virtual ValueTask<string?> ResolveGetTableNameAsync(long id, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    /// <summary>
    /// 解析插入物理表名。
    /// </summary>
    protected virtual ValueTask<string?> ResolveInsertTableNameAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    /// <summary>
    /// 解析更新物理表名。
    /// </summary>
    protected virtual ValueTask<string?> ResolveUpdateTableNameAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    /// <summary>
    /// 解析删除物理表名。
    /// </summary>
    protected virtual ValueTask<string?> ResolveDeleteTableNameAsync(long id, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    /// <summary>
    /// 插入前处理。
    /// </summary>
    protected virtual Task BeforeInsertAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 插入后处理。
    /// </summary>
    protected virtual Task AfterInsertAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 更新前处理。
    /// </summary>
    protected virtual Task BeforeUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 更新后处理。
    /// </summary>
    protected virtual Task AfterUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 应用查询表名。
    /// </summary>
    protected static ISelect<TEntity> ApplyTable(ISelect<TEntity> query, string? tableName) =>
        string.IsNullOrWhiteSpace(tableName)
            ? query
            : query.AsTable((_, _) => tableName);

    /// <summary>
    /// 应用插入表名。
    /// </summary>
    protected static IInsert<TEntity> ApplyTable(IInsert<TEntity> command, string? tableName) =>
        string.IsNullOrWhiteSpace(tableName)
            ? command
            : command.AsTable(tableName);

    /// <summary>
    /// 应用更新表名。
    /// </summary>
    protected static IUpdate<TEntity> ApplyTable(IUpdate<TEntity> command, string? tableName) =>
        string.IsNullOrWhiteSpace(tableName)
            ? command
            : command.AsTable(tableName);

    /// <summary>
    /// 应用删除表名。
    /// </summary>
    protected static IDelete<TEntity> ApplyTable(IDelete<TEntity> command, string? tableName) =>
        string.IsNullOrWhiteSpace(tableName)
            ? command
            : command.AsTable(tableName);
}
using FreeSql;
using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// FreeSql 模块仓储基类。
/// </summary>
/// <typeparam name="TDbContext">模块数据库上下文类型。</typeparam>
public abstract class FreeSqlModuleRepository<TDbContext>
    where TDbContext : IHarborModuleDbContext
{
    private readonly TDbContext _db;

    /// <summary>
    /// 初始化模块仓储。
    /// </summary>
    protected FreeSqlModuleRepository(TDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 模块数据库上下文。
    /// </summary>
    protected TDbContext DbContext => _db;

    /// <summary>
    /// 当前模块 ORM。
    /// 使用的模块默认数据库，如果所操作实体使用了<see cref="OverrideDbKeyAttribute"/>则必须显式用
    /// <c>DbEntityRegistry.GetDbKey&lt;TEntity&gt;()</c>
    /// 获取实体最终 DbKey，再调用 <c>db.GetOrm(dbKey)</c>。
    /// </summary>
    protected IFreeSql FreeSql => _db.Orm;

    /// <summary>
    /// 获取实体仓储。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="cascadeSave">是否启用级联保存。</param>
    /// <returns>FreeSql 实体仓储。</returns>
    protected IBaseRepository<TEntity> GetRepository<TEntity>(bool cascadeSave = false)
        where TEntity : class
    {
        var repository = FreeSql.GetRepository<TEntity>();
        repository.DbContextOptions.EnableCascadeSave = cascadeSave;
        return repository;
    }

    /// <summary>
    /// 插入实体并回填雪花 ID。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="entity">待插入实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>插入后的实体。</returns>
    protected async Task<TEntity> InsertAndFillIdAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : EntityBase
    {
        var inserted = await FreeSql.Insert(entity).ExecuteInsertedAsync(cancellationToken);
        var saved = inserted.FirstOrDefault();
        if (saved is not null)
        {
            entity.Id = saved.Id;
        }

        return entity;
    }
}

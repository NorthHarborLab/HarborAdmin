using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.BuildingBlocks.Abstractions.Repositories;

/// <summary>
/// Harbor 基础 CRUD 仓储契约
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IHarborCrudRepository<TEntity> : IHarborQueryRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>
    /// 插入实体
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存后的实体</returns>
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存后的实体</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// 物理删除实体
    /// </summary>
    /// <param name="id">实体主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 软删除实体
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken);
}
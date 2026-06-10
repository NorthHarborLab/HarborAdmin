using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Repositories;

/// <summary>
/// Harbor 基础 CRUD 仓储契约。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public interface IHarborCrudRepository<TEntity>
    where TEntity : EntityBase
{
    /// <summary>
    /// 查询列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实体列表。</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页实体列表。</returns>
    Task<PagedResult<TEntity>> PageAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键查询。
    /// </summary>
    /// <param name="id">实体主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实体；不存在时返回空。</returns>
    Task<TEntity?> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入实体。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存后的实体。</returns>
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存后的实体。</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 物理删除实体。
    /// </summary>
    /// <param name="id">实体主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除实体。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SoftDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
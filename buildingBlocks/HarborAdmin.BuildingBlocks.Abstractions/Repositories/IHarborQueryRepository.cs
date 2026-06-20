using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.BuildingBlocks.Abstractions.Repositories;

/// <summary>
/// Harbor 基础查询仓储契约
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IHarborQueryRepository<TEntity> : IHarborRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>
    /// 查询列表
    /// </summary>
    /// <param name="options">查询选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(HarborQueryOptions? options, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="options">查询选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页实体列表</returns>
    Task<PagedResult<TEntity>> PageAsync(HarborQueryOptions? options, CancellationToken cancellationToken);

    /// <summary>
    /// 按主键查询
    /// </summary>
    /// <param name="id">实体主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体；不存在时返回空</returns>
    Task<TEntity?> GetAsync(long id, CancellationToken cancellationToken);
}

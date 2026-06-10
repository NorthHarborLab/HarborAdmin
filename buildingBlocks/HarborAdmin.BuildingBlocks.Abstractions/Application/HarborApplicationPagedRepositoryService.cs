using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor Repository 驱动的基础分页 CRUD 应用服务。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
/// <typeparam name="TRepository">实体仓储类型。</typeparam>
public abstract class HarborApplicationPagedRepositoryService<TEntity, TDto, TQuery, TSaveRequest, TRepository>
    : HarborApplicationRepositoryService<TEntity, TDto, TSaveRequest, TRepository>, IPagedCrudApplicationService<TDto, TQuery, TSaveRequest>
    where TEntity : EntityBase, new()
    where TQuery : PageRequest
    where TRepository : IHarborCrudRepository<TEntity>
{
    /// <summary>
    /// 初始化 Repository 驱动的分页 CRUD 应用服务。
    /// </summary>
    /// <param name="repository">实体仓储。</param>
    protected HarborApplicationPagedRepositoryService(TRepository repository) : base(repository)
    {
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TDto>> PageAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var result = await Repository.PageAsync(query, cancellationToken);
        return PagedResult<TDto>.From(result.Items.Select(MapToDto).ToList(), result.Total);
    }
}
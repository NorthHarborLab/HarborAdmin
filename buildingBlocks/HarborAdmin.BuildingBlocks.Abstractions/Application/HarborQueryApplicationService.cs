using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor Repository 驱动的查询应用服务。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TRepository">实体仓储类型。</typeparam>
public abstract class HarborQueryApplicationService<TEntity, TDto, TQuery, TRepository>
    : HarborApplicationService, IHarborQueryApplicationService<TDto, TQuery>
    where TEntity : EntityBase, new()
    where TQuery : PageRequest
    where TRepository : IHarborQueryRepository<TEntity>
{
    /// <summary>
    /// 初始化 Repository 驱动的查询应用服务。
    /// </summary>
    /// <param name="repository">实体仓储。</param>
    protected HarborQueryApplicationService(TRepository repository)
    {
        Repository = repository;
    }

    /// <summary>
    /// 实体仓储。
    /// </summary>
    protected TRepository Repository { get; }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TDto>> ListAsync(CancellationToken cancellationToken) =>
        (await Repository.ListAsync(HarborQueryOptions.Empty, cancellationToken))
        .Select(MapToDto)
        .ToList();

    /// <inheritdoc />
    public virtual async Task<TDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var entity = RequireFound(await Repository.GetAsync(id, cancellationToken), GetNotFoundMessage(id));
        return MapToDto(entity);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TDto>> PageAsync(TQuery query, CancellationToken cancellationToken)
    {
        var options = CreateQueryOptions(query);
        var result = await Repository.PageAsync(options, cancellationToken);
        return PagedResult<TDto>.From(result.Items.Select(MapToDto).ToList(), result.Total);
    }

    /// <summary>
    /// 映射实体到 DTO。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <returns>DTO。</returns>
    protected abstract TDto MapToDto(TEntity entity);

    /// <summary>
    /// 将控制器分页请求转换为仓储查询选项。
    /// </summary>
    /// <param name="query">控制器分页查询请求。</param>
    /// <returns>仓储查询选项。</returns>
    protected virtual HarborQueryOptions CreateQueryOptions(TQuery query) =>
        new()
        {
            Page = query.Page,
            PageSize = query.PageSize,
            SortField = query.SortField,
            SortOrder = query.SortOrder,
            Filters = query.Filters,
        };

    /// <summary>
    /// 构造未找到消息。
    /// </summary>
    protected virtual string GetNotFoundMessage(long id) => $"{typeof(TEntity).Name} '{id}' was not found.";
}

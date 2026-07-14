using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Results;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor 查询应用服务契约。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
public interface IHarborQueryApplicationService<TDto, in TQuery> : IHarborApplicationService
    where TQuery : PageRequest
{
    /// <summary>
    /// 查询列表。
    /// </summary>
    Task<HarborResult<IReadOnlyList<TDto>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 查询详情。
    /// </summary>
    Task<HarborResult<TDto>> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 分页查询。
    /// </summary>
    /// <param name="query">分页查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页查询结果。</returns>
    Task<HarborResult<PagedResult<TDto>>> PageAsync(TQuery query, CancellationToken cancellationToken);
}

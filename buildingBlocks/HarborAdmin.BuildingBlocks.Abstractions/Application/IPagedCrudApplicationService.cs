using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// 基础分页 CRUD 应用服务契约。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public interface IPagedCrudApplicationService<TDto, in TQuery, in TSaveRequest> : ICrudApplicationService<TDto, TSaveRequest>
    where TQuery : PageRequest
{
    /// <summary>
    /// 分页查询。
    /// </summary>
    /// <param name="query">分页查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页查询结果。</returns>
    Task<PagedResult<TDto>> PageAsync(TQuery query, CancellationToken cancellationToken = default);
}

using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.BuildingBlocks.AspNetCore.Controllers;

/// <summary>
/// Harbor 查询 Controller 基类。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <remarks>
/// 与查询应用服务、查询仓储的类型层级对应；具体 Action 直接调用应用服务。
/// </remarks>
public abstract class HarborQueryControllerBase<TDto, TQuery> : HarborControllerBase
    where TQuery : PageRequest
{
    /// <summary>
    /// 执行标准列表查询。
    /// </summary>
    /// <param name="service">查询应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一列表响应。</returns>
    protected static async Task<ActionResult<ApiResult<IReadOnlyList<TDto>>>> ListResultAsync(
        IHarborQueryApplicationService<TDto, TQuery> service,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await service.ListAsync(cancellationToken));
    }

    /// <summary>
    /// 执行标准详情查询。
    /// </summary>
    /// <param name="id">实体主键。</param>
    /// <param name="service">查询应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一详情响应。</returns>
    protected static async Task<ActionResult<ApiResult<TDto>>> GetResultAsync(
        long id,
        IHarborQueryApplicationService<TDto, TQuery> service,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetAsync(id, cancellationToken));
    }

    /// <summary>
    /// 执行标准分页查询。
    /// </summary>
    /// <param name="query">分页查询请求。</param>
    /// <param name="service">查询应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一分页响应。</returns>
    protected static async Task<ActionResult<ApiResult<PagedResult<TDto>>>> PageResultAsync(
        TQuery query,
        IHarborQueryApplicationService<TDto, TQuery> service,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await service.PageAsync(query, cancellationToken));
    }
}

using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Controllers;

/// <summary>
/// 基础分页 CRUD Controller 响应包装。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public abstract class PagedCrudControllerBase<TDto, TQuery, TSaveRequest> : CrudControllerBase<TDto, TSaveRequest>
    where TQuery : PageRequest
{
    /// <summary>
    /// 执行分页查询并包装统一响应。
    /// </summary>
    /// <param name="query">分页查询请求。</param>
    /// <param name="service">分页 CRUD 应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一分页响应。</returns>
    protected static async Task<ApiResult<PagedResult<TDto>>> PageResultAsync(TQuery query, IPagedCrudApplicationService<TDto, TQuery, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return await OkResultAsync(service.PageAsync(query, cancellationToken));
    }
}

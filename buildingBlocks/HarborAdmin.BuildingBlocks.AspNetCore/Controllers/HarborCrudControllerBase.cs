using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.BuildingBlocks.AspNetCore.Controllers;

/// <summary>
/// Harbor CRUD Controller 基类。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
/// <remarks>
/// 与 CRUD 应用服务、CRUD 仓储的类型层级对应；具体 Action 直接调用应用服务。
/// </remarks>
public abstract class HarborCrudControllerBase<TDto, TQuery, TSaveRequest> : HarborQueryControllerBase<TDto, TQuery>
    where TQuery : PageRequest
{
    /// <summary>
    /// 执行标准创建操作。
    /// </summary>
    /// <param name="request">保存请求。</param>
    /// <param name="service">CRUD 应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一创建响应。</returns>
    protected static async Task<ActionResult<ApiResult<TDto>>> CreateResultAsync(
        TSaveRequest request,
        IHarborCrudApplicationService<TDto, TQuery, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await service.SaveAsync(null, request, cancellationToken));
    }

    /// <summary>
    /// 执行标准更新操作。
    /// </summary>
    /// <param name="id">实体主键。</param>
    /// <param name="request">保存请求。</param>
    /// <param name="service">CRUD 应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一更新响应。</returns>
    protected static async Task<ActionResult<ApiResult<TDto>>> UpdateResultAsync(
        long id,
        TSaveRequest request,
        IHarborCrudApplicationService<TDto, TQuery, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await service.SaveAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// 执行标准删除操作。
    /// </summary>
    /// <param name="id">实体主键。</param>
    /// <param name="service">CRUD 应用服务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>统一删除响应。</returns>
    protected static async Task<ActionResult<ApiResult<bool>>> DeleteResultAsync(
        long id,
        IHarborCrudApplicationService<TDto, TQuery, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await service.DeleteAsync(id, cancellationToken));
    }
}

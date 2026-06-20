using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Controllers;

/// <summary>
/// 基础 CRUD Controller 响应包装。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public abstract class CrudControllerBase<TDto, TQuery, TSaveRequest> : PagedCrudControllerBase<TDto, TQuery>
    where TQuery : PageRequest
{
    /// <summary>
    /// 执行创建并包装统一响应。
    /// </summary>
    protected static Task<ApiResult<TDto>> CreateResultAsync(TSaveRequest request, IHarborCrudApplicationService<TDto, TQuery, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return CreateResultAsync<TSaveRequest, TDto>(request, cancellationToken, (body, token) => service.SaveAsync(null, body, token));
    }

    /// <summary>
    /// 执行更新并包装统一响应。
    /// </summary>
    protected static Task<ApiResult<TDto>> UpdateResultAsync(long id, TSaveRequest request, IHarborCrudApplicationService<TDto, TQuery, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return UpdateResultAsync<long, TSaveRequest, TDto>(id, request, cancellationToken, (entityId, body, token) => service.SaveAsync(entityId, body, token));
    }

    /// <summary>
    /// 执行删除并包装统一响应。
    /// </summary>
    protected static Task<ApiResult<bool>> DeleteResultAsync(long id, IHarborCrudApplicationService<TDto, TQuery, TSaveRequest> service, CancellationToken cancellationToken)
    {
        return DeleteResultAsync<long>(id, cancellationToken, service.DeleteAsync);
    }
}

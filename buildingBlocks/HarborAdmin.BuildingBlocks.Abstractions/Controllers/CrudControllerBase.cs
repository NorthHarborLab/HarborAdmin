using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.Abstractions.Controllers;

/// <summary>
/// 基础 CRUD Controller 响应包装。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public abstract class CrudControllerBase<TDto, TSaveRequest> : HarborControllerBase
{
    /// <summary>
    /// 执行列表查询并包装统一响应。
    /// </summary>
    protected static async Task<ApiResult<IReadOnlyList<TDto>>> ListResultAsync(ICrudApplicationService<TDto, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return await ListResultAsync(cancellationToken, service.ListAsync);
    }

    /// <summary>
    /// 执行详情查询并包装统一响应。
    /// </summary>
    protected static async Task<ApiResult<TDto>> GetResultAsync(long id, ICrudApplicationService<TDto, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return await OkResultAsync(service.GetAsync(id, cancellationToken));
    }

    /// <summary>
    /// 执行创建并包装统一响应。
    /// </summary>
    protected static Task<ApiResult<TDto>> CreateResultAsync(TSaveRequest request, ICrudApplicationService<TDto, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return CreateResultAsync<TSaveRequest, TDto>(request, cancellationToken, (body, token) => service.SaveAsync(null, body, token));
    }

    /// <summary>
    /// 执行更新并包装统一响应。
    /// </summary>
    protected static Task<ApiResult<TDto>> UpdateResultAsync(long id, TSaveRequest request, ICrudApplicationService<TDto, TSaveRequest> service,
        CancellationToken cancellationToken)
    {
        return UpdateResultAsync<long, TSaveRequest, TDto>(id, request, cancellationToken, (entityId, body, token) => service.SaveAsync(entityId, body, token));
    }

    /// <summary>
    /// 执行删除并包装统一响应。
    /// </summary>
    protected static Task<ApiResult<bool>> DeleteResultAsync(long id, ICrudApplicationService<TDto, TSaveRequest> service, CancellationToken cancellationToken)
    {
        return DeleteResultAsync<long>(id, cancellationToken, service.DeleteAsync);
    }
}

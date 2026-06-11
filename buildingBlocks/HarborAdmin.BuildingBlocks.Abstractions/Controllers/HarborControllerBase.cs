using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.BuildingBlocks.Abstractions.Controllers;

/// <summary>
/// Harbor Controller 通用响应包装基类。
/// </summary>
public abstract class HarborControllerBase : ControllerBase
{
    /// <summary>
    /// 包装成功响应。
    /// </summary>
    protected static ApiResult<TValue> OkResult<TValue>(TValue value) => ApiResult.Ok(value);

    /// <summary>
    /// 执行业务任务并包装成功响应。
    /// </summary>
    protected static async Task<ApiResult<TValue>> OkResultAsync<TValue>(Task<TValue> task)
    {
        return ApiResult.Ok(await task);
    }

    /// <summary>
    /// 执行业务任务并包装成功响应。
    /// </summary>
    protected static async Task<ApiResult<TValue>> OkResultAsync<TValue>(CancellationToken cancellationToken, Func<CancellationToken, Task<TValue>> action)
    {
        return ApiResult.Ok(await action(cancellationToken));
    }

    /// <summary>
    /// 执行列表查询并包装成功响应。
    /// </summary>
    protected static Task<ApiResult<IReadOnlyList<TItem>>> ListResultAsync<TItem>(CancellationToken cancellationToken,
        Func<CancellationToken, Task<IReadOnlyList<TItem>>> list)
    {
        return OkResultAsync(cancellationToken, list);
    }

    /// <summary>
    /// 执行分页查询并包装成功响应。
    /// </summary>
    protected static Task<ApiResult<PagedResult<TItem>>> PageResultAsync<TItem>(CancellationToken cancellationToken,
        Func<CancellationToken, Task<PagedResult<TItem>>> page)
    {
        return OkResultAsync(cancellationToken, page);
    }

    /// <summary>
    /// 执行创建并包装成功响应。
    /// </summary>
    protected static async Task<ApiResult<TResult>> CreateResultAsync<TRequest, TResult>(TRequest request, CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResult>> create)
    {
        return ApiResult.Ok(await create(request, cancellationToken));
    }

    /// <summary>
    /// 执行更新并包装成功响应。
    /// </summary>
    protected static async Task<ApiResult<TResult>> UpdateResultAsync<TKey, TRequest, TResult>(TKey key, TRequest request, CancellationToken cancellationToken,
        Func<TKey, TRequest, CancellationToken, Task<TResult>> update)
    {
        return ApiResult.Ok(await update(key, request, cancellationToken));
    }

    /// <summary>
    /// 执行删除并包装成功响应。
    /// </summary>
    protected static async Task<ApiResult<bool>> DeleteResultAsync<TKey>(TKey key, CancellationToken cancellationToken, Func<TKey, CancellationToken, Task> delete)
    {
        await delete(key, cancellationToken);
        return ApiResult.Ok(true);
    }
}
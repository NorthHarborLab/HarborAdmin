using HarborAdmin.BuildingBlocks.Abstractions.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// 将控制器返回的裸 DTO 包装为 <see cref="ApiResult{T}"/>，与前端 Vben 约定对齐。
/// </summary>
public sealed class ApiResultFilter : IAsyncResultFilter
{
    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            if (!IsAlreadyWrapped(objectResult.Value))
            {
                var wrapped = WrapValue(objectResult.Value);
                objectResult.Value = wrapped;
            }
        }

        await next();
    }

    private static bool IsAlreadyWrapped(object value) =>
        value is ApiResult || value.GetType().IsGenericType
            && value.GetType().GetGenericTypeDefinition() == typeof(ApiResult<>);

    private static object WrapValue(object value)
    {
        var resultType = typeof(ApiResult<>).MakeGenericType(value.GetType());
        var ok = resultType.GetMethod(
            nameof(ApiResult<object>.Ok),
            [value.GetType(), typeof(string)])!;
        return ok.Invoke(null, [value, null])!;
    }
}

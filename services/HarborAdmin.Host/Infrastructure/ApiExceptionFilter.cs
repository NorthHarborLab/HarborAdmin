using HarborAdmin.BuildingBlocks.Abstractions.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// 将常见业务异常映射为 <see cref="ApiResult"/> 与合适的 HTTP 状态码。
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        var (statusCode, code, message) = context.Exception switch
        {
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ApiResultCodes.NotFound, ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, ApiResultCodes.BadRequest, ex.Message),
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, ApiResultCodes.BadRequest, ex.Message),
            _ => (0, 0, null),
        };

        if (statusCode == 0 || message is null)
        {
            return;
        }

        context.Result = new ObjectResult(ApiResult.Fail(code, message))
        {
            StatusCode = statusCode,
        };
        context.ExceptionHandled = true;
    }
}

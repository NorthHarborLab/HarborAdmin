using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.BuildingBlocks.AspNetCore.Controllers;

/// <summary>
/// Harbor Controller 根基类。
/// </summary>
/// <remarks>
/// 仅建立 Controller 类型层级与公共扩展点，不封装具体 Action 调用或响应创建流程。
/// </remarks>
public abstract class HarborControllerBase : ControllerBase
{
    /// <summary>
    /// 将协议无关的应用结果映射为 HTTP API 响应。
    /// </summary>
    protected static ActionResult<ApiResult<T>> ToActionResult<T>(HarborResult<T> result)
    {
        if (result.IsSuccess)
        {
            return ApiResult.Ok(result.Value!);
        }

        var error = result.Error!;
        var status = GetHttpStatus(error.Kind);
        return new ObjectResult(ApiResult<T>.Fail(
            status,
            error.DefaultMessage,
            error.FieldErrors,
            error.Metadata,
            error.Code,
            error.Kind.ToString(),
            error.Arguments,
            error.Retryable))
        {
            StatusCode = status,
        };
    }

    /// <summary>
    /// 获取错误分类对应的 HTTP 状态码。
    /// </summary>
    private static int GetHttpStatus(HarborErrorKind kind) => kind switch
    {
        HarborErrorKind.Validation => StatusCodes.Status400BadRequest,
        HarborErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        HarborErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        HarborErrorKind.NotFound => StatusCodes.Status404NotFound,
        HarborErrorKind.Conflict => StatusCodes.Status409Conflict,
        HarborErrorKind.Business => StatusCodes.Status400BadRequest,
        HarborErrorKind.RateLimited => StatusCodes.Status429TooManyRequests,
        HarborErrorKind.DependencyUnavailable => StatusCodes.Status503ServiceUnavailable,
        HarborErrorKind.Timeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError,
    };
}

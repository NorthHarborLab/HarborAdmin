using HarborAdmin.BuildingBlocks.Abstractions.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// 将模型校验失败统一转为 ApiResult 契约。
/// </summary>
public sealed class ApiValidationFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors
                        .Select(item => string.IsNullOrWhiteSpace(item.ErrorMessage) ? item.Exception?.Message : item.ErrorMessage)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item!)
                        .Distinct()
                        .ToArray());

            var message = errors.SelectMany(item => item.Value)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) is { Length: > 0 } firstMessage
                ? firstMessage
                : "请求参数不合法。";

            context.Result = new ObjectResult(ApiResult.Fail(ApiResultCodes.BadRequest, message, errors))
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };

            return Task.CompletedTask;
        }

        return next();
    }
}

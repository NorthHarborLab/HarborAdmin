using System.Collections;
using System.Reflection;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HarborAdmin.Host.Filter;

/// <summary>
/// 根据功能字段权限统一裁剪响应数据。
/// </summary>
public sealed class FieldPermissionResultFilter(ICurrentUser currentUser, AdminRuntimeAccessService accessService, AdminFieldProjectionService projectionService)
    : IAsyncResultFilter
{
    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (ShouldSkipFieldProjection(context.HttpContext) || currentUser.Id <= 0)
        {
            await next();
            return;
        }

        var api = await accessService.ResolveApiAsync(
            context.HttpContext.Request.Path.Value ?? string.Empty,
            context.HttpContext.Request.Method,
            context.HttpContext.RequestAborted);
        if (api is null || string.IsNullOrWhiteSpace(api.FeatureCode))
        {
            await next();
            return;
        }

        if (context.Result is ObjectResult { Value: { } value })
        {
            var dataProperty = value.GetType().GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
            var codeProperty = value.GetType().GetProperty("Code", BindingFlags.Public | BindingFlags.Instance);
            var code = codeProperty?.GetValue(value) as int? ?? 0;
            if (dataProperty is not null && dataProperty is { CanRead: true, CanWrite: true } && code == 0)
            {
                var data = dataProperty.GetValue(value);
                var surface = ResolveSurface(context, data);
                var accessSet = await accessService.GetFieldPermissionsAsync(
                    currentUser.Id,
                    api.FeatureCode,
                    surface,
                    context.HttpContext.RequestAborted);
                dataProperty.SetValue(value, projectionService.Project(data, accessSet));
            }
        }

        await next();
    }

    /// <summary>
    /// 判断当前 Endpoint 是否不参与字段权限裁剪。
    /// </summary>
    private static bool ShouldSkipFieldProjection(HttpContext context)
    {
        var metadata = context.GetEndpoint()?.Metadata;
        return metadata?.GetMetadata<IAllowAnonymous>() is not null
               || metadata?.GetMetadata<AuthenticatedOnlyAttribute>() is not null;
    }

    /// <summary>
    /// 根据 HTTP 方法与返回数据形态推断字段权限作用面。
    /// </summary>
    private static AdminFieldSurface ResolveSurface(ResultExecutingContext context, object? data)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method))
        {
            return IsCollectionLike(data) ? AdminFieldSurface.List : AdminFieldSurface.Detail;
        }

        if (HttpMethods.IsPost(method) &&
            context.HttpContext.Request.Path.Value?.Contains("/query", StringComparison.OrdinalIgnoreCase) == true)
        {
            return AdminFieldSurface.List;
        }

        return AdminFieldSurface.Detail;
    }

    /// <summary>
    /// 判断返回数据是否为集合或分页列表形态。
    /// </summary>
    private static bool IsCollectionLike(object? data)
    {
        return data switch
        {
            null or string => false,
            IEnumerable => true,
            _ => data.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance) is not null
        };
    }
}
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Request;

namespace HarborAdmin.Modules.Admin.Application.Services.DynamicCrud;

/// <summary>
/// Admin 动态 CRUD 应用服务。
/// </summary>
public sealed class AdminDynamicCrudService(IAdminDynamicResourceHandlerResolver handlerResolver)
{
    /// <summary>
    /// 查询动态资源记录。
    /// </summary>
    public async Task<DynamicQueryResultDto> QueryAsync(
        string featureCode,
        DynamicQueryRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationDomainException("动态查询请求不能为空。");
        }

        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        return await handler.QueryAsync(NormalizeQueryRequest(request), cancellationToken);
    }

    /// <summary>
    /// 获取动态资源记录详情。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> GetAsync(
        string featureCode,
        string id,
        CancellationToken cancellationToken)
    {
        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        return await handler.GetAsync(id, cancellationToken)
               ?? throw new NotFoundDomainException($"Dynamic record '{id}' was not found.");
    }

    /// <summary>
    /// 新增动态资源记录。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> CreateAsync(
        string featureCode,
        IReadOnlyDictionary<string, object?>? values,
        CancellationToken cancellationToken)
    {
        if (values is null)
        {
            throw new ValidationDomainException("动态记录数据不能为空。");
        }

        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        return await handler.CreateAsync(values, cancellationToken);
    }

    /// <summary>
    /// 更新动态资源记录。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> UpdateAsync(
        string featureCode,
        string id,
        IReadOnlyDictionary<string, object?>? values,
        CancellationToken cancellationToken)
    {
        if (values is null)
        {
            throw new ValidationDomainException("动态记录数据不能为空。");
        }

        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        return await handler.UpdateAsync(id, values, cancellationToken);
    }

    /// <summary>
    /// 删除动态资源记录。
    /// </summary>
    public async Task DeleteAsync(string featureCode, string id, CancellationToken cancellationToken)
    {
        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        await handler.DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// 规范化动态功能编码。
    /// </summary>
    private static string NormalizeFeatureCode(string featureCode)
    {
        var normalized = featureCode.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ValidationDomainException("动态功能编码不能为空。")
            : normalized;
    }

    /// <summary>
    /// 规范化动态查询分页、搜索和排序参数。
    /// </summary>
    private static DynamicQueryRequest NormalizeQueryRequest(DynamicQueryRequest request)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 200);
        return request with
        {
            Page = page,
            PageSize = pageSize,
            Search = request.Search ?? new Dictionary<string, object?>(),
            Sorts = request.Sorts ?? Array.Empty<DynamicSortItem>(),
        };
    }
}

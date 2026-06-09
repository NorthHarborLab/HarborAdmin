using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Request;

namespace HarborAdmin.Modules.Admin.Application.Services.DynamicCrud;

/// <summary>
/// Admin 动态 CRUD 应用服务。
/// </summary>
public sealed class AdminDynamicCrudService(
    IAdminDynamicResourceHandlerResolver handlerResolver,
    ICurrentUser currentUser,
    AdminRuntimeAccessService accessService,
    AdminFieldProjectionService projectionService,
    AdminFieldInputValidator inputValidator)
{
    /// <summary>
    /// 查询动态资源记录。
    /// </summary>
    public async Task<DynamicQueryResultDto> QueryAsync(string featureCode, DynamicQueryRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationDomainException("动态查询请求不能为空。");
        }

        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        var result = await handler.QueryAsync(NormalizeQueryRequest(request), cancellationToken);
        var accessSet = await GetFieldAccessAsync(featureCode, AdminFieldSurface.List, cancellationToken);
        var items = result.Items
            .Select(item => ProjectDictionary(item, accessSet))
            .ToArray();
        return new DynamicQueryResultDto(items, result.Total);
    }

    /// <summary>
    /// 获取动态资源记录详情。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> GetAsync(string featureCode, string id, CancellationToken cancellationToken)
    {
        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        var record = await handler.GetAsync(id, cancellationToken)
                     ?? throw new NotFoundDomainException($"Dynamic record '{id}' was not found.");
        var accessSet = await GetFieldAccessAsync(featureCode, AdminFieldSurface.Detail, cancellationToken);
        return ProjectDictionary(record, accessSet);
    }

    /// <summary>
    /// 新增动态资源记录。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> CreateAsync(string featureCode, IReadOnlyDictionary<string, object?>? values,
        CancellationToken cancellationToken)
    {
        if (values is null)
        {
            throw new ValidationDomainException("动态记录数据不能为空。");
        }

        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        await inputValidator.EnsureEditableAsync(currentUser.Id, NormalizeFeatureCode(featureCode), values, AdminFieldSurface.Create, cancellationToken);
        var result = await handler.CreateAsync(values, cancellationToken);
        var accessSet = await GetFieldAccessAsync(featureCode, AdminFieldSurface.Detail, cancellationToken);
        return ProjectDictionary(result, accessSet);
    }

    /// <summary>
    /// 更新动态资源记录。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> UpdateAsync(string featureCode, string id, IReadOnlyDictionary<string, object?>? values,
        CancellationToken cancellationToken)
    {
        if (values is null)
        {
            throw new ValidationDomainException("动态记录数据不能为空。");
        }

        var handler = await handlerResolver.ResolveAsync(NormalizeFeatureCode(featureCode), cancellationToken);
        await inputValidator.EnsureEditableAsync(currentUser.Id, NormalizeFeatureCode(featureCode), values, AdminFieldSurface.Update, cancellationToken);
        var result = await handler.UpdateAsync(id, values, cancellationToken);
        var accessSet = await GetFieldAccessAsync(featureCode, AdminFieldSurface.Detail, cancellationToken);
        return ProjectDictionary(result, accessSet);
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
        return new DynamicQueryRequest(Page: page, PageSize: pageSize, Search: request.Search ?? new Dictionary<string, object?>(),
            Sorts: request.Sorts ?? []);
    }

    /// <summary>
    /// 获取当前用户在动态功能下的字段访问权限。
    /// </summary>
    private async Task<AdminFieldPermissionSet> GetFieldAccessAsync(string featureCode, AdminFieldSurface surface, CancellationToken cancellationToken)
    {
        if (currentUser.Id <= 0)
        {
            throw new UnauthorizedDomainException("未登录或登录已过期。");
        }

        return await accessService.GetFieldPermissionsAsync(currentUser.Id, NormalizeFeatureCode(featureCode), surface, cancellationToken);
    }

    /// <summary>
    /// 按字段访问权限裁剪动态记录字典。
    /// </summary>
    private IReadOnlyDictionary<string, object?> ProjectDictionary(IReadOnlyDictionary<string, object?> record, AdminFieldPermissionSet accessSet)
    {
        if (accessSet.IsSuperAdmin)
        {
            return record;
        }

        return (IReadOnlyDictionary<string, object?>)projectionService.Project(record, accessSet)!;
    }
}
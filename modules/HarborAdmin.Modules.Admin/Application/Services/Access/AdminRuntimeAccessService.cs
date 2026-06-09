using HarborAdmin.Modules.Admin.Infrastructure.Caching;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// Admin 运行时权限服务，所有资源关系均从数据库元数据解析。
/// </summary>
public sealed class AdminRuntimeAccessService(AccessCacheService accessCache)
{
    /// <summary>
    /// 根据请求路径和 HTTP 方法解析数据库中定义的 API 元数据。
    /// </summary>
    public async Task<ApiAuthorizationEndpointCacheItem?> ResolveApiAsync(string path, string method, CancellationToken cancellationToken)
    {
        var endpoints = await accessCache.GetApiAuthorizationMapAsync(cancellationToken);
        var normalizedMethod = method.ToUpperInvariant();
        var normalizedPath = NormalizePath(path);
        return endpoints
            .Where(api => api.HttpMethod == normalizedMethod)
            .OrderByDescending(api => ExactSegmentCount(api.Path))
            .FirstOrDefault(api => AccessPathMatcher.Matches(NormalizePath(api.Path), normalizedPath));
    }

    /// <summary>
    /// 获取用户在指定 Feature 下的字段权限集合。
    /// </summary>
    public async Task<AdminFieldPermissionSet> GetFieldPermissionsAsync(long userId, string featureCode, AdminFieldSurface surface, CancellationToken cancellationToken = default)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        if (snapshot.IsSuperAdmin)
        {
            return AdminFieldPermissionSet.Full;
        }

        var policies = snapshot.FieldPolicies
            .Where(policy => string.Equals(policy.FeatureCode, featureCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var visible = policies
            .Where(policy => policy.Visible && IsAvailableForSurface(policy.Visible, policy.Editable, policy.Exportable, surface))
            .Select(policy => policy.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var editable = policies
            .Where(policy => policy is { Visible: true, Editable: true })
            .Select(policy => policy.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var exportable = policies
            .Where(policy => policy is { Visible: true, Exportable: true })
            .Select(policy => policy.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var masked = policies
            .Where(policy => policy.Masked)
            .Select(policy => policy.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AdminFieldPermissionSet(false, visible, editable, exportable, masked);
    }

    /// <summary>
    /// 判断字段在指定作用面下是否可用。
    /// </summary>
    private static bool IsAvailableForSurface(bool visible, bool editable, bool exportable, AdminFieldSurface surface) =>
        surface switch
        {
            AdminFieldSurface.Create or AdminFieldSurface.Update => visible && editable,
            AdminFieldSurface.Export => visible && exportable,
            _ => visible,
        };

    /// <summary>
    /// 规范化请求路径，去除前端代理和服务端路由间的 <c>/api</c> 前缀差异。
    /// </summary>
    private static string NormalizePath(string path)
    {
        var normalized = "/" + path.Trim('/');
        return normalized.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? normalized["/api".Length..]
            : normalized;
    }

    /// <summary>
    /// 计算路径模板中的固定片段数量，用于优先匹配更具体的接口。
    /// </summary>
    private static int ExactSegmentCount(string path) =>
        NormalizePath(path)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Count(segment => !(segment.StartsWith('{') && segment.EndsWith('}')));
}

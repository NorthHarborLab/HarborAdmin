namespace HarborAdmin.Modules.Admin.Application.Services.Access;

using Infrastructure.Caching;

/// <summary>
/// API 访问鉴权服务。
/// </summary>
public sealed class ApiAuthorizationService(AccessCacheService accessCache)
{
    /// <summary>
    /// 判断用户是否允许访问指定 API。
    /// </summary>
    public async Task<bool> CanAccessApiAsync(long userId, string path, string method, CancellationToken cancellationToken)
    {
        var snapshot = await accessCache.GetUserSnapshotAsync(userId, cancellationToken);
        var normalizedMethod = method.ToUpperInvariant();
        var normalizedPath = NormalizePath(path);
        var endpoints = await accessCache.GetApiAuthorizationMapAsync(cancellationToken);
        var endpoint = ResolveEndpoint(endpoints, normalizedMethod, normalizedPath);
        if (endpoint is null)
        {
            // 直接执行 SQL 种子不会走 BumpSessionVersionAsync，此处刷新一次运行时授权图。
            await accessCache.InvalidateRuntimeAccessAsync(cancellationToken);
            endpoints = await accessCache.GetApiAuthorizationMapAsync(cancellationToken);
            endpoint = ResolveEndpoint(endpoints, normalizedMethod, normalizedPath);
        }

        if (endpoint is null)
        {
            return snapshot.IsSuperAdmin;
        }

        if (snapshot.IsSuperAdmin)
        {
            return true;
        }

        return endpoint.RequiredPermissionCodes.Length > 0
               && endpoint.RequiredPermissionCodes.Any(permission => snapshot.Permissions.Contains(permission));
    }

    /// <summary>
    /// 在授权端点列表中解析与请求匹配的 API。
    /// </summary>
    private static ApiAuthorizationEndpointCacheItem? ResolveEndpoint(
        IReadOnlyList<ApiAuthorizationEndpointCacheItem> endpoints,
        string normalizedMethod,
        string normalizedPath) =>
        endpoints
            .Where(api => api.HttpMethod == normalizedMethod)
            .OrderByDescending(api => ExactSegmentCount(api.Path))
            .FirstOrDefault(api => AccessPathMatcher.Matches(NormalizePath(api.Path), normalizedPath));

    /// <summary>
    /// 规范化请求路径，消除 <c>/api</c> 前缀差异。
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

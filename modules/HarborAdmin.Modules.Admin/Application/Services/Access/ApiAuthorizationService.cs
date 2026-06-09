namespace HarborAdmin.Modules.Admin.Application.Services.Access;

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
        var endpoints = await accessCache.GetApiAuthorizationMapAsync(cancellationToken);
        var normalizedMethod = method.ToUpperInvariant();
        var normalizedPath = NormalizePath(path);
        var endpoint = endpoints
            .Where(api => api.HttpMethod == normalizedMethod)
            .OrderByDescending(api => ExactSegmentCount(api.Path))
            .FirstOrDefault(api => AccessPathMatcher.Matches(NormalizePath(api.Path), normalizedPath));
        if (endpoint is null)
        {
            return false;
        }

        if (snapshot.IsSuperAdmin)
        {
            return true;
        }

        return endpoint.RequiredPermissionCodes.Length > 0
               && endpoint.RequiredPermissionCodes.Any(permission => snapshot.Permissions.Contains(permission));
    }

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

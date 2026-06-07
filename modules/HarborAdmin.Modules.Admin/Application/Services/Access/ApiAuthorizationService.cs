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
        if (snapshot.IsSuperAdmin)
        {
            return true;
        }

        var apis = await accessCache.GetEnabledFeatureApisAsync(cancellationToken);
        var endpoint = apis
            .Where(api => api.HttpMethod == method.ToUpperInvariant())
            .FirstOrDefault(api => AccessPathMatcher.Matches(api.Path, path));
        if (endpoint is null)
        {
            return true;
        }

        var actionLinks = await accessCache.GetFeatureActionApiLinksAsync(endpoint.Id, cancellationToken);
        if (actionLinks.Count == 0)
        {
            return true;
        }

        var actionKeys = actionLinks
            .Select(link => $"{link.FeatureCode}\u001F{link.ActionCode}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var featureCodes = actionLinks.Select(link => link.FeatureCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var actions = (await accessCache.GetEnabledFeatureActionsAsync(cancellationToken))
            .Where(action => featureCodes.Contains(action.FeatureCode))
            .Where(action => actionKeys.Contains($"{action.FeatureCode}\u001F{action.ActionCode}"))
            .ToList();
        if (actions.Count == 0)
        {
            return true;
        }

        return actions.Any(action => snapshot.Permissions.Contains(action.PermissionCode));
    }
}

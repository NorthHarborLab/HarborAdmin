using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Authorization;

/// <summary>
/// API 访问鉴权服务。
/// </summary>
public sealed class ApiAuthorizationService(AdminServiceContext context, AccessQueryService accessQuery)
{
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 判断用户是否允许访问指定 API。
    /// </summary>
    public async Task<bool> CanAccessApiAsync(long userId, string path, string method, CancellationToken cancellationToken)
    {
        var roles = await accessQuery.GetEnabledUserRolesAsync(userId, cancellationToken);
        if (roles.Any(role => role.RoleCode == "super_admin"))
        {
            return true;
        }

        var endpoint = (await Orm.Select<AdminFeatureApi>()
                .Where(api => api.Enabled && api.HttpMethod == method.ToUpperInvariant())
                .ToListAsync(cancellationToken))
            .FirstOrDefault(api => AccessQueryService.PathMatches(api.Path, path));
        if (endpoint is null)
        {
            return true;
        }

        var actionLinks = await Orm.Select<AdminFeatureActionApi>()
            .Where(link => link.AdminFeatureApiId == endpoint.Id)
            .ToListAsync(cancellationToken);
        if (actionLinks.Count == 0)
        {
            return true;
        }

        var actionKeys = actionLinks
            .Select(link => $"{link.FeatureCode}\u001F{link.ActionCode}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var featureCodes = actionLinks.Select(link => link.FeatureCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var actions = await Orm.Select<AdminFeatureAction>()
            .Where(action => action.Enabled && featureCodes.Contains(action.FeatureCode))
            .ToListAsync(cancellationToken);
        actions = actions
            .Where(action => actionKeys.Contains($"{action.FeatureCode}\u001F{action.ActionCode}"))
            .ToList();
        if (actions.Count == 0)
        {
            return true;
        }

        var userPermissions = await accessQuery.GetUserPermissionsAsync(userId, cancellationToken);
        return actions.Any(action => userPermissions.Contains(action.PermissionCode));
    }
}

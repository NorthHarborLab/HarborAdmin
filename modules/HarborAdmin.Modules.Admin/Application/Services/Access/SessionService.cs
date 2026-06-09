using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using System.Text.Json;
using HarborAdmin.Modules.Admin.Application.Services.Dictionary;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Application.Services.Menu;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// 用户会话快照服务。
/// </summary>
public sealed class SessionService(
    AdminServiceContext context,
    AccessCacheService accessCache,
    AccessQueryService accessQuery,
    FieldPolicyService fieldPolicyService,
    AdminFieldOptionResolver optionResolver,
    UserService userService)
{
    /// <summary>
    /// 解析字段选项和校验 JSON 时使用的序列化配置。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 构建当前用户的会话访问包，包含用户信息、权限、路由与字段策略。
    /// </summary>
    public async Task<SessionSnapshotDto> BuildSessionAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedDomainException("未登录或登录已过期。");
        }

        var user = await userService.GetUserAsync(userId, cancellationToken)
                   ?? throw new NotFoundDomainException("用户不存在。");
        var roles = await accessQuery.GetEnabledUserRolesAsync(userId, cancellationToken);
        var permissions = await accessQuery.GetUserPermissionsAsync(userId, cancellationToken);
        var menus = await accessQuery.GetUserMenusAsync(userId, cancellationToken);
        var routes = await MenuMapper.BuildRoutesAsync(context.Orm, menus, permissions, cancellationToken);
        var fieldPolicies = await fieldPolicyService.GetFieldPoliciesForUserAsync(userId, cancellationToken);
        var dataScopes = await accessQuery.GetDataScopesAsync(roles, cancellationToken);
        var sessionVersion = await accessCache.GetSessionVersionAsync(cancellationToken);
        var resources = await BuildResourcesAsync(menus, permissions, fieldPolicies, user.IsSuperAdmin, cancellationToken);
        var homePath = user.HomePath;
        if (string.IsNullOrWhiteSpace(homePath) || menus.All(menu => menu.RoutePath != homePath))
        {
            homePath = menus.FirstOrDefault(menu => menu.MenuType != "catalog")?.RoutePath ?? "/dashboard";
        }

        return new SessionSnapshotDto(
            sessionVersion,
            new CurrentUserDto(
                user.Id.ToString(),
                user.UserName,
                user.DisplayName,
                user.Avatar ?? string.Empty,
                user.Remark ?? string.Empty,
                homePath,
                roles.Select(role => role.RoleCode).ToArray(),
                user.IsSuperAdmin),
            permissions,
            routes,
            fieldPolicies,
            dataScopes,
            resources,
            homePath);
    }

    /// <summary>
    /// 获取全局 sessionVersion，供前端判断权限/菜单是否需要刷新。
    /// </summary>
    public async Task<SessionVersionDto> GetSessionVersionAsync(CancellationToken cancellationToken) =>
        new(await accessCache.GetSessionVersionAsync(cancellationToken));

    /// <summary>
    /// 根据用户菜单、权限码和字段策略构建前端功能资源包。
    /// </summary>
    private async Task<IReadOnlyList<FeatureResourceDto>> BuildResourcesAsync(
        IReadOnlyList<AdminMenu> menus,
        IReadOnlyList<string> permissions,
        IReadOnlyList<FieldPolicyDto> fieldPolicies,
        bool isSuperAdmin,
        CancellationToken cancellationToken)
    {
        var featureCodes = menus
            .Select(menu => menu.FeatureCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (featureCodes.Length == 0)
        {
            return [];
        }

        var permissionSet = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var policyMap = fieldPolicies
            .GroupBy(policy => $"{policy.FeatureCode}\u001F{policy.FieldName}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
        var resources = new List<FeatureResourceDto>(featureCodes.Length);
        foreach (var featureCode in featureCodes)
        {
            var schema = await accessCache.GetFeatureRuntimeSchemaAsync(featureCode, cancellationToken);
            var fields = schema.Fields
                .Where(field => field.Enabled)
                .OrderBy(field => field.SortOrder)
                .Where(field => isSuperAdmin || policyMap.TryGetValue($"{schema.FeatureCode}\u001F{field.FieldCode}", out var policy) && policy.Visible)
                .ToArray();
            var resourceFields = new List<FeatureResourceFieldDto>(fields.Length);
            foreach (var field in fields)
            {
                resourceFields.Add(await ToResourceFieldAsync(schema.FeatureCode, field, isSuperAdmin, policyMap, cancellationToken));
            }

            var actions = schema.Actions
                .Where(action => action.Enabled)
                .Where(action => isSuperAdmin || permissionSet.Contains(action.PermissionCode))
                .OrderBy(action => action.SortOrder)
                .Select(action => new FeatureResourceActionDto(
                    action.ActionCode,
                    action.LabelKey,
                    action.LabelFallback,
                    action.PermissionCode,
                    action.SortOrder))
                .ToArray();
            var endpoints = schema.Apis
                .OrderBy(api => api.ApiCode)
                .Select(api => new FeatureResourceEndpointDto(api.ApiCode, api.Path, api.HttpMethod))
                .ToArray();

            resources.Add(new FeatureResourceDto(
                schema.FeatureCode,
                schema.Name,
                schema.FeatureType,
                schema.Component,
                schema.RoutePath,
                schema.SchemaVersion,
                resourceFields,
                actions,
                endpoints));
        }

        return resources;
    }

    /// <summary>
    /// 将字段缓存项转换为当前用户可用的前端字段资源。
    /// </summary>
    private async Task<FeatureResourceFieldDto> ToResourceFieldAsync(
        string featureCode,
        FeatureFieldCacheItem field,
        bool isSuperAdmin,
        IReadOnlyDictionary<string, FieldPolicyDto> policyMap,
        CancellationToken cancellationToken)
    {
        var hasPolicy = policyMap.TryGetValue($"{featureCode}\u001F{field.FieldCode}", out var policy);
        var visible = isSuperAdmin || hasPolicy && policy!.Visible;
        var editable = isSuperAdmin || hasPolicy && policy!.Editable;
        var exportable = isSuperAdmin || hasPolicy && policy!.Exportable;
        var masked = !isSuperAdmin && hasPolicy && policy!.Masked;
        return new FeatureResourceFieldDto(
            field.FieldCode,
            field.LabelKey,
            field.LabelFallback,
            field.PlaceholderKey,
            field.PlaceholderFallback,
            field.Component,
            field.DataType,
            field.Required,
            field.Readonly || !editable,
            field.SortOrder,
            field.Width,
            field.ListVisible,
            field.SearchVisible,
            field.CreateVisible,
            field.UpdateVisible,
            visible,
            editable,
            exportable,
            masked,
            field.DictCode,
            await optionResolver.ResolveResourceOptionsAsync(field, cancellationToken),
            ParseValidation(field.ValidationJson));
    }

    /// <summary>
    /// 解析字段校验规则 JSON。
    /// </summary>
    private static JsonElement? ParseValidation(string? validationJson)
    {
        if (string.IsNullOrWhiteSpace(validationJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(validationJson, JsonOptions);
    }
}

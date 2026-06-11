using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// Admin 访问控制缓存编排服务。
/// </summary>
public sealed class AccessCacheService(
    IHarborCache cache,
    IHarborCacheInvalidator cacheInvalidator,
    IAdminAccessRepository repository,
    IAdminFeatureDesignRepository featureDesignRepository,
    AdminServiceContext context)
{
    /// <summary>
    /// 获取全局 sessionVersion（带缓存）。
    /// </summary>
    public async Task<long> GetSessionVersionAsync(CancellationToken cancellationToken)
    {
        var model = await cache.Get<SessionVersionCacheModel>()
            .Where(item => item.VersionKey == AdminAccessCacheKeys.SessionVersionId)
            .GetOrCreateAsync(async ct => new SessionVersionCacheModel
            {
                VersionKey = AdminAccessCacheKeys.SessionVersionId,
                Version = await context.GetSessionVersionValueAsync(ct),
            }, cancellationToken);
        return model.Version;
    }

    /// <summary>
    /// 获取用户访问快照。
    /// </summary>
    public async Task<UserAccessSnapshotCacheModel> GetUserSnapshotAsync(long userId, CancellationToken cancellationToken)
    {
        var sessionVersion = await GetSessionVersionAsync(cancellationToken);
        return await cache.Get<UserAccessSnapshotCacheModel>()
            .Where(item => item.UserId == userId && item.SessionVersion == sessionVersion)
            .GetOrCreateAsync(ct => LoadUserSnapshotAsync(userId, sessionVersion, ct), cancellationToken);
    }

    /// <summary>
    /// 获取已启用 Feature API 列表。
    /// </summary>
    public async Task<IReadOnlyList<FeatureApiCacheItem>> GetEnabledFeatureApisAsync(CancellationToken cancellationToken)
    {
        var model = await cache.Get<EnabledFeatureApisCacheModel>()
            .Where(item => item.ItemKey == AdminAccessCacheKeys.FeatureApisKey)
            .GetOrCreateAsync(async ct =>
            {
                var apis = await repository.GetEnabledFeatureApisAsync(ct);
                return new EnabledFeatureApisCacheModel
                {
                    ItemKey = AdminAccessCacheKeys.FeatureApisKey,
                    Apis = apis.Select(api => new FeatureApiCacheItem(api.Id, api.FeatureCode, api.ApiCode, api.Path, api.HttpMethod)).ToArray(),
                };
            }, cancellationToken);
        return model.Apis;
    }

    /// <summary>
    /// 获取已启用 Feature Action 列表。
    /// </summary>
    public async Task<IReadOnlyList<FeatureActionCacheItem>> GetEnabledFeatureActionsAsync(CancellationToken cancellationToken)
    {
        var model = await cache.Get<EnabledFeatureActionsCacheModel>()
            .Where(item => item.ItemKey == AdminAccessCacheKeys.FeatureActionsKey)
            .GetOrCreateAsync(async ct =>
            {
                var actions = await repository.GetEnabledFeatureActionsAsync(ct);
                return new EnabledFeatureActionsCacheModel
                {
                    ItemKey = AdminAccessCacheKeys.FeatureActionsKey,
                    Actions = actions.Select(action => new FeatureActionCacheItem(
                        action.FeatureCode,
                        action.ActionCode,
                        action.PermissionCode,
                        action.LabelKey,
                        action.LabelFallback,
                        action.SortOrder,
                        action.Enabled)).ToArray(),
                };
            }, cancellationToken);
        return model.Actions;
    }

    /// <summary>
    /// 获取 API 授权图。
    /// </summary>
    public async Task<IReadOnlyList<ApiAuthorizationEndpointCacheItem>> GetApiAuthorizationMapAsync(CancellationToken cancellationToken)
    {
        var model = await cache.Get<ApiAuthorizationMapCacheModel>()
            .Where(item => item.ItemKey == AdminAccessCacheKeys.ApiAuthorizationMapKey)
            .GetOrCreateAsync(async ct =>
            {
                var apis = await GetEnabledFeatureApisAsync(ct);
                var actions = await GetEnabledFeatureActionsAsync(ct);
                var actionMap = actions.ToDictionary(
                    action => $"{action.FeatureCode}\u001F{action.ActionCode}",
                    action => action.PermissionCode,
                    StringComparer.OrdinalIgnoreCase);
                var endpoints = new List<ApiAuthorizationEndpointCacheItem>(apis.Count);

                foreach (var api in apis)
                {
                    var links = await GetFeatureActionApiLinksAsync(api.Id, ct);
                    var requiredPermissions = links
                        .Select(link => actionMap.GetValueOrDefault($"{link.FeatureCode}\u001F{link.ActionCode}"))
                        .Where(permission => !string.IsNullOrWhiteSpace(permission))
                        .Select(permission => permission!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    endpoints.Add(new ApiAuthorizationEndpointCacheItem(
                        api.Id,
                        api.FeatureCode,
                        api.ApiCode,
                        api.Path,
                        api.HttpMethod,
                        requiredPermissions));
                }

                return new ApiAuthorizationMapCacheModel
                {
                    ItemKey = AdminAccessCacheKeys.ApiAuthorizationMapKey,
                    Endpoints = endpoints.ToArray(),
                };
            }, cancellationToken);

        return model.Endpoints;
    }

    /// <summary>
    /// 获取指定 API 绑定的 Action 链接。
    /// </summary>
    public async Task<IReadOnlyList<FeatureActionApiLinkCacheItem>> GetFeatureActionApiLinksAsync(long featureApiId, CancellationToken cancellationToken)
    {
        var model = await cache.Get<FeatureActionApiLinksCacheModel>()
            .Where(item => item.FeatureApiId == featureApiId)
            .GetOrCreateAsync(async ct =>
            {
                var links = await repository.GetFeatureActionApiLinksAsync(featureApiId, ct);
                return new FeatureActionApiLinksCacheModel
                {
                    FeatureApiId = featureApiId,
                    Links = links.Select(link => new FeatureActionApiLinkCacheItem(link.FeatureCode, link.ActionCode)).ToArray(),
                };
            }, cancellationToken);
        return model.Links;
    }

    /// <summary>
    /// 获取角色权限码列表。
    /// </summary>
    public async Task<IReadOnlyList<string>> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken)
    {
        var model = await cache.Get<RolePermissionsCacheModel>()
            .Where(item => item.RoleId == roleId)
            .GetOrCreateAsync(async ct =>
            {
                var links = await repository.GetRolePermissionLinksAsync([roleId], ct);
                return new RolePermissionsCacheModel
                {
                    RoleId = roleId,
                    PermissionCodes = links.Select(link => link.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                };
            }, cancellationToken);
        return model.PermissionCodes;
    }

    /// <summary>
    /// 获取角色菜单 ID 列表。
    /// </summary>
    public async Task<IReadOnlyList<long>> GetRoleMenuIdsAsync(long roleId, CancellationToken cancellationToken)
    {
        var model = await cache.Get<RoleMenuIdsCacheModel>()
            .Where(item => item.RoleId == roleId)
            .GetOrCreateAsync(async ct =>
            {
                var links = await repository.GetRoleMenuLinksAsync([roleId], ct);
                return new RoleMenuIdsCacheModel
                {
                    RoleId = roleId,
                    MenuIds = links.Select(link => link.MenuId).Distinct().ToArray(),
                };
            }, cancellationToken);
        return model.MenuIds;
    }

    /// <summary>
    /// 获取角色数据范围列表。
    /// </summary>
    public async Task<IReadOnlyList<RoleDataScopeCacheItem>> GetRoleDataScopesAsync(long roleId, CancellationToken cancellationToken)
    {
        var model = await cache.Get<RoleDataScopesCacheModel>()
            .Where(item => item.RoleId == roleId)
            .GetOrCreateAsync(async ct =>
            {
                var scopes = await repository.GetRoleDataScopesAsync([roleId], ct);
                return new RoleDataScopesCacheModel
                {
                    RoleId = roleId,
                    Scopes = scopes.Select(scope => new RoleDataScopeCacheItem(
                        scope.ScopeType,
                        scope.ScopeValueType,
                        scope.ScopeValueId)).ToArray(),
                };
            }, cancellationToken);
        return model.Scopes;
    }

    /// <summary>
    /// 获取角色字段策略列表。
    /// </summary>
    public async Task<IReadOnlyList<FieldPolicyDto>> GetRoleFieldPoliciesAsync(long roleId, CancellationToken cancellationToken)
    {
        var model = await cache.Get<RoleFieldPoliciesCacheModel>()
            .Where(item => item.RoleId == roleId)
            .GetOrCreateAsync(async ct =>
            {
                var policies = await repository.GetRoleFieldPoliciesAsync(roleId, ct);
                return new RoleFieldPoliciesCacheModel
                {
                    RoleId = roleId,
                    Policies = policies.Select(policy => new FieldPolicyDto(
                        policy.FeatureCode,
                        policy.FieldName,
                        policy.Visible,
                        policy.Editable,
                        policy.Exportable,
                        policy.Masked)).ToArray(),
                };
            }, cancellationToken);
        return model.Policies;
    }

    /// <summary>
    /// 合并多角色字段策略（OR 并集）。
    /// </summary>
    public async Task<IReadOnlyList<FieldPolicyDto>> MergeRoleFieldPoliciesAsync(
        IReadOnlyList<long> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var merged = new List<FieldPolicyDto>();
        foreach (var roleId in roleIds)
        {
            merged.AddRange(await GetRoleFieldPoliciesAsync(roleId, cancellationToken));
        }

        return merged
            .GroupBy(policy => (policy.FeatureCode, policy.FieldName))
            .Select(group =>
            {
                var items = group.ToArray();
                return new FieldPolicyDto(
                    group.Key.FeatureCode,
                    group.Key.FieldName,
                    items.Any(item => item.Visible),
                    items.Any(item => item.Editable),
                    items.Any(item => item.Exportable),
                    items.Any(item => item.Masked));
            })
            .ToArray();
    }

    /// <summary>
    /// 获取 Feature 运行时 schema。
    /// </summary>
    public async Task<FeatureRuntimeSchemaCacheModel> GetFeatureRuntimeSchemaAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        return await cache.Get<FeatureRuntimeSchemaCacheModel>()
            .Where(item => item.FeatureCode == normalized)
            .GetOrCreateAsync(async ct =>
            {
                var feature = await featureDesignRepository.GetEnabledFeatureRuntimeAsync(normalized, ct)
                              ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
                return new FeatureRuntimeSchemaCacheModel
                {
                    FeatureCode = feature.FeatureCode,
                    Name = feature.Name,
                    FeatureType = feature.FeatureType,
                    Component = feature.Component,
                    RoutePath = feature.RoutePath,
                    SchemaVersion = feature.SchemaVersion,
                    Fields = feature.Fields.Select(field => new FeatureFieldCacheItem(
                        field.FieldCode,
                        field.LabelKey,
                        field.LabelFallback,
                        field.PlaceholderKey,
                        field.PlaceholderFallback,
                        field.Component,
                        field.DataType,
                        field.Required,
                        field.Readonly,
                        field.SortOrder,
                        field.Width,
                        field.ListVisible,
                        field.SearchVisible,
                        field.CreateVisible,
                        field.UpdateVisible,
                        field.Enabled,
                        field.DictCode,
                        field.OptionsJson,
                        field.ValidationJson)).ToArray(),
                    Actions = feature.Actions.Select(action => new FeatureActionCacheItem(
                        action.FeatureCode,
                        action.ActionCode,
                        action.PermissionCode,
                        action.LabelKey,
                        action.LabelFallback,
                        action.SortOrder,
                        action.Enabled)).ToArray(),
                    Apis = feature.Apis.Select(api => new FeatureApiCacheItem(api.Id, api.FeatureCode, api.ApiCode, api.Path, api.HttpMethod)).ToArray(),
                };
            }, cancellationToken);
    }

    /// <summary>
    /// 失效全部用户访问快照与 sessionVersion 缓存。
    /// </summary>
    public async Task InvalidateUserAccessAsync(CancellationToken cancellationToken)
    {
        await cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.AllUsersTag, cancellationToken);
        await cache.Get<SessionVersionCacheModel>()
            .Where(item => item.VersionKey == AdminAccessCacheKeys.SessionVersionId)
            .RemoveAsync(cancellationToken);
    }

    /// <summary>
    /// 失效 Feature/API 运行时授权缓存（直接改库后需调用或重启 Host）。
    /// </summary>
    public ValueTask InvalidateRuntimeAccessAsync(CancellationToken cancellationToken) =>
        cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.RuntimeTag, cancellationToken);

    /// <summary>
    /// 失效全部角色维度缓存。
    /// </summary>
    public ValueTask InvalidateAllRolesAccessAsync(CancellationToken cancellationToken) =>
        cacheInvalidator.InvalidateTagAsync(AdminAccessCacheKeys.AllRolesTag, cancellationToken);

    /// <summary>
    /// 从数据库加载用户访问快照。
    /// </summary>
    private async ValueTask<UserAccessSnapshotCacheModel> LoadUserSnapshotAsync(long userId, long sessionVersion, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is not { Enabled: true })
        {
            return new UserAccessSnapshotCacheModel
            {
                UserId = userId,
                SessionVersion = sessionVersion,
            };
        }

        if (user.IsSuperAdmin)
        {
            // 超级管理员不受数据范围限制，菜单和权限来自全部启用资源。
            var allActions = await repository.GetEnabledFeatureActionsAsync(cancellationToken);
            var allMenus = await repository.ListEnabledMenusAsync(cancellationToken);
            return new UserAccessSnapshotCacheModel
            {
                UserId = userId,
                SessionVersion = sessionVersion,
                IsSuperAdmin = true,
                Permissions = allActions.Select(action => action.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                Menus = allMenus.Select(ToMenuCacheItem).ToArray(),
                Roles = [],
                AllowedDepartmentIds = null,
                FieldPolicies = [],
            };
        }

        var userRoles = await repository.GetUserRoleLinksAsync(userId, cancellationToken);
        if (userRoles.Count == 0)
        {
            return new UserAccessSnapshotCacheModel
            {
                UserId = userId,
                SessionVersion = sessionVersion,
            };
        }

        var roleIds = userRoles.Select(link => link.RoleId).ToArray();
        var roles = await repository.GetRolesByIdsAsync(roleIds, enabledOnly: true, cancellationToken);
        if (roles.Count == 0)
        {
            return new UserAccessSnapshotCacheModel
            {
                UserId = userId,
                SessionVersion = sessionVersion,
            };
        }

        var enabledRoleIds = roles.Select(role => role.Id).ToArray();
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var menuIds = new HashSet<long>();
        foreach (var roleId in enabledRoleIds)
        {
            foreach (var permission in await GetRolePermissionsAsync(roleId, cancellationToken))
            {
                permissions.Add(permission);
            }

            foreach (var menuId in await GetRoleMenuIdsAsync(roleId, cancellationToken))
            {
                menuIds.Add(menuId);
            }
        }

        var menus = menuIds.Count == 0
            ? []
            : await repository.ListEnabledMenusByIdsAsync(menuIds.ToArray(), cancellationToken);

        return new UserAccessSnapshotCacheModel
        {
            UserId = userId,
            SessionVersion = sessionVersion,
            Permissions = permissions.ToArray(),
            Menus = menus.Select(ToMenuCacheItem).ToArray(),
            Roles = roles.Select(role => new UserRoleCacheItem(role.Id, role.RoleCode, role.DataScopeType, role.Enabled)).ToArray(),
            AllowedDepartmentIds = await ComputeAllowedDepartmentIdsAsync(user, roles, cancellationToken),
            FieldPolicies = (await MergeRoleFieldPoliciesAsync(enabledRoleIds, cancellationToken)).ToArray(),
        };
    }

    /// <summary>
    /// 根据角色数据范围计算用户可访问部门集合。
    /// </summary>
    private async Task<long[]?> ComputeAllowedDepartmentIdsAsync(AdminUser user, IReadOnlyList<AdminRole> roles, CancellationToken cancellationToken)
    {
        if (roles.Any(role => role.DataScopeType == "All"))
        {
            // null 表示无限制，比空数组更能表达 All 范围。
            return null;
        }

        if (user.DeptId is null)
        {
            return [];
        }

        if (roles.Any(role => role.DataScopeType is "DeptWithChildren" or "SelfWithSubordinates"))
        {
            var departments = await repository.ListDepartmentsAsync(cancellationToken);
            var ids = new HashSet<long> { user.DeptId.Value };
            AddChildDepartmentIds(user.DeptId.Value, departments, ids);
            return ids.ToArray();
        }

        if (roles.Any(role => role.DataScopeType == "Dept"))
        {
            return [user.DeptId.Value];
        }

        return [];
    }

    /// <summary>
    /// 递归追加指定部门的所有子部门 ID。
    /// </summary>
    private static void AddChildDepartmentIds(long parentId, IReadOnlyList<AdminDepartment> departments, ISet<long> ids)
    {
        foreach (var child in departments.Where(dept => dept.ParentId == parentId))
        {
            if (ids.Add(child.Id))
            {
                AddChildDepartmentIds(child.Id, departments, ids);
            }
        }
    }

    internal static UserMenuCacheItem ToMenuCacheItem(AdminMenu menu) =>
        new(
            menu.Id,
            menu.ParentId,
            menu.RoutePath,
            menu.RouteName,
            menu.MenuType,
            menu.FeatureCode,
            menu.PermissionCode,
            menu.SortOrder,
            menu.Title,
            menu.Icon,
            menu.ActiveIcon,
            menu.ActivePath,
            menu.AffixTab,
            menu.AffixTabOrder,
            menu.HideInTab,
            menu.KeepAlive,
            menu.HideChildrenInMenu,
            menu.Link,
            menu.IframeSrc,
            menu.OpenInNewWindow,
            menu.MetaJson,
            menu.Visible,
            menu.Enabled);

    internal static AdminMenu ToAdminMenu(UserMenuCacheItem item) =>
        new()
        {
            Id = item.Id,
            ParentId = item.ParentId,
            RoutePath = item.RoutePath,
            RouteName = item.RouteName,
            MenuType = item.MenuType,
            FeatureCode = item.FeatureCode,
            PermissionCode = item.PermissionCode,
            SortOrder = item.SortOrder,
            Title = item.Title,
            Icon = item.Icon,
            ActiveIcon = item.ActiveIcon,
            ActivePath = item.ActivePath,
            AffixTab = item.AffixTab,
            AffixTabOrder = item.AffixTabOrder,
            HideInTab = item.HideInTab,
            KeepAlive = item.KeepAlive,
            HideChildrenInMenu = item.HideChildrenInMenu,
            Link = item.Link,
            IframeSrc = item.IframeSrc,
            OpenInNewWindow = item.OpenInNewWindow,
            MetaJson = item.MetaJson,
            Visible = item.Visible,
            Enabled = item.Enabled,
        };

    internal static AdminRole ToAdminRole(UserRoleCacheItem item) =>
        new()
        {
            Id = item.Id,
            RoleCode = item.RoleCode,
            DataScopeType = item.DataScopeType,
            Enabled = item.Enabled,
        };
}

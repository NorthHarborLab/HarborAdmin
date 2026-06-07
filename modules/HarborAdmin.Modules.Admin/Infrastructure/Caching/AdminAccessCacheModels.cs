using HarborAdmin.BuildingBlocks.Caching.Attributes;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// 用户菜单缓存项。
/// </summary>
public sealed record UserMenuCacheItem(
    long Id,
    long? ParentId,
    string RoutePath,
    string RouteName,
    string MenuType,
    string? FeatureCode,
    string? PermissionCode,
    int SortOrder,
    string Title,
    string? Icon,
    string? MetaJson,
    bool Visible,
    bool Enabled);

/// <summary>
/// 用户角色缓存项。
/// </summary>
public sealed record UserRoleCacheItem(
    long Id,
    string RoleCode,
    string DataScopeType,
    bool Enabled);

/// <summary>
/// 角色数据范围缓存项。
/// </summary>
public sealed record RoleDataScopeCacheItem(
    string ScopeType,
    string? ScopeValueType,
    long? ScopeValueId);

/// <summary>
/// Feature API 缓存项。
/// </summary>
public sealed record FeatureApiCacheItem(
    long Id,
    string ApiCode,
    string Path,
    string HttpMethod);

/// <summary>
/// Feature Action 缓存项。
/// </summary>
public sealed record FeatureActionCacheItem(
    string FeatureCode,
    string ActionCode,
    string PermissionCode,
    string LabelKey,
    string? LabelFallback,
    int SortOrder,
    bool Enabled);

/// <summary>
/// Feature 字段缓存项。
/// </summary>
public sealed record FeatureFieldCacheItem(
    string FieldCode,
    string LabelKey,
    string? LabelFallback,
    string? PlaceholderKey,
    string? PlaceholderFallback,
    string Component,
    string DataType,
    bool Required,
    bool Readonly,
    int SortOrder,
    int? Width,
    bool ListVisible,
    bool SearchVisible,
    bool CreateVisible,
    bool UpdateVisible,
    bool Enabled,
    string? OptionsJson,
    string? ValidationJson);

/// <summary>
/// Feature Action-API 链接缓存项。
/// </summary>
public sealed record FeatureActionApiLinkCacheItem(
    string FeatureCode,
    string ActionCode);

/// <summary>
/// 全局 sessionVersion 缓存模型。
/// </summary>
[CacheCatalog("会话版本", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 10, Description = "全局 sessionVersion")]
[CacheKey("harbor:admin:access:session-version", Key = "{VersionKey}", ExpirationSeconds = 300)]
public sealed class SessionVersionCacheModel
{
    /// <summary>
    /// 固定 key 段。
    /// </summary>
    [CacheKeyPart]
    public string VersionKey { get; init; } = AdminAccessCacheKeys.SessionVersionId;

    /// <summary>
    /// 当前版本号。
    /// </summary>
    public long Version { get; init; }
}

/// <summary>
/// 用户访问快照缓存模型。
/// </summary>
[CacheCatalog("用户访问快照", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 20, Description = "用户菜单/权限/角色快照")]
[CacheKey("harbor:admin:access:user", Key = "{UserId}:{SessionVersion}", ExpirationSeconds = 1800)]
[CacheTag(AdminAccessCacheKeys.AllUsersTag)]
[CacheTag("harbor:admin:access:user:{UserId}", typeof(AdminUser), typeof(AdminUserRole))]
[CacheTag(AdminAccessCacheKeys.AllRolesTag, typeof(AdminRole), typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
public sealed class UserAccessSnapshotCacheModel
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    [CacheKeyPart]
    public long UserId { get; init; }

    /// <summary>
    /// 会话版本号。
    /// </summary>
    [CacheKeyPart]
    public long SessionVersion { get; init; }

    /// <summary>
    /// 是否超级管理员。
    /// </summary>
    public bool IsSuperAdmin { get; init; }

    /// <summary>
    /// 权限码集合。
    /// </summary>
    public string[] Permissions { get; init; } = [];

    /// <summary>
    /// 可访问菜单。
    /// </summary>
    public UserMenuCacheItem[] Menus { get; init; } = [];

    /// <summary>
    /// 用户角色。
    /// </summary>
    public UserRoleCacheItem[] Roles { get; init; } = [];

    /// <summary>
    /// 可访问部门 ID；null 表示不限制。
    /// </summary>
    public long[]? AllowedDepartmentIds { get; init; }

    /// <summary>
    /// 字段策略。
    /// </summary>
    public FieldPolicyDto[] FieldPolicies { get; init; } = [];
}

/// <summary>
/// 已启用 Feature API 列表缓存模型。
/// </summary>
[CacheCatalog("Feature API 列表", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 30)]
[CacheKey("harbor:admin:access:runtime", Key = "{ItemKey}", ExpirationSeconds = 600)]
[CacheTag(AdminAccessCacheKeys.RuntimeTag, typeof(AdminFeatureApi))]
public sealed class EnabledFeatureApisCacheModel
{
    /// <summary>
    /// 固定 key 段。
    /// </summary>
    [CacheKeyPart]
    public string ItemKey { get; init; } = AdminAccessCacheKeys.FeatureApisKey;

    /// <summary>
    /// API 列表。
    /// </summary>
    public FeatureApiCacheItem[] Apis { get; init; } = [];
}

/// <summary>
/// 已启用 Feature Action 列表缓存模型。
/// </summary>
[CacheCatalog("Feature Action 列表", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 31)]
[CacheKey("harbor:admin:access:runtime", Key = "{ItemKey}", ExpirationSeconds = 600)]
[CacheTag(AdminAccessCacheKeys.RuntimeTag, typeof(AdminFeatureAction))]
public sealed class EnabledFeatureActionsCacheModel
{
    /// <summary>
    /// 固定 key 段。
    /// </summary>
    [CacheKeyPart]
    public string ItemKey { get; init; } = AdminAccessCacheKeys.FeatureActionsKey;

    /// <summary>
    /// Action 列表。
    /// </summary>
    public FeatureActionCacheItem[] Actions { get; init; } = [];
}

/// <summary>
/// Feature Action-API 链接缓存模型。
/// </summary>
[CacheCatalog("Feature Action-API 链接", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 32)]
[CacheKey("harbor:admin:access:runtime", Key = "action-api:{FeatureApiId}", ExpirationSeconds = 600)]
[CacheTag(AdminAccessCacheKeys.RuntimeTag, typeof(AdminFeatureActionApi))]
public sealed class FeatureActionApiLinksCacheModel
{
    /// <summary>
    /// Feature API ID。
    /// </summary>
    [CacheKeyPart]
    public long FeatureApiId { get; init; }

    /// <summary>
    /// 链接列表。
    /// </summary>
    public FeatureActionApiLinkCacheItem[] Links { get; init; } = [];
}

/// <summary>
/// 角色权限缓存模型。
/// </summary>
[CacheCatalog("角色权限", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 40)]
[CacheKey("harbor:admin:access:role", Key = "role-permissions:{RoleId}", ExpirationSeconds = 1800)]
[CacheTag(AdminAccessCacheKeys.AllRolesTag, typeof(AdminRole), typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleTagTemplate, typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleIdTagTemplate, typeof(AdminRole))]
public sealed class RolePermissionsCacheModel
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    [CacheKeyPart]
    public long RoleId { get; init; }

    /// <summary>
    /// 权限码列表。
    /// </summary>
    public string[] PermissionCodes { get; init; } = [];
}

/// <summary>
/// 角色菜单 ID 缓存模型。
/// </summary>
[CacheCatalog("角色菜单", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 41)]
[CacheKey("harbor:admin:access:role", Key = "role-menus:{RoleId}", ExpirationSeconds = 1800)]
[CacheTag(AdminAccessCacheKeys.AllRolesTag, typeof(AdminRole), typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleTagTemplate, typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleIdTagTemplate, typeof(AdminRole))]
public sealed class RoleMenuIdsCacheModel
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    [CacheKeyPart]
    public long RoleId { get; init; }

    /// <summary>
    /// 菜单 ID 列表。
    /// </summary>
    public long[] MenuIds { get; init; } = [];
}

/// <summary>
/// 角色数据范围缓存模型。
/// </summary>
[CacheCatalog("角色数据范围", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 42)]
[CacheKey("harbor:admin:access:role", Key = "role-data-scopes:{RoleId}", ExpirationSeconds = 1800)]
[CacheTag(AdminAccessCacheKeys.AllRolesTag, typeof(AdminRole), typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleTagTemplate, typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleIdTagTemplate, typeof(AdminRole))]
public sealed class RoleDataScopesCacheModel
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    [CacheKeyPart]
    public long RoleId { get; init; }

    /// <summary>
    /// 数据范围列表。
    /// </summary>
    public RoleDataScopeCacheItem[] Scopes { get; init; } = [];
}

/// <summary>
/// 角色字段策略缓存模型。
/// </summary>
[CacheCatalog("角色字段策略", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 43)]
[CacheKey("harbor:admin:access:role", Key = "role-field-policies:{RoleId}", ExpirationSeconds = 1800)]
[CacheTag(AdminAccessCacheKeys.AllRolesTag, typeof(AdminRole), typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleTagTemplate, typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
[CacheTag(AdminAccessCacheKeys.RoleIdTagTemplate, typeof(AdminRole))]
public sealed class RoleFieldPoliciesCacheModel
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    [CacheKeyPart]
    public long RoleId { get; init; }

    /// <summary>
    /// 字段策略列表。
    /// </summary>
    public FieldPolicyDto[] Policies { get; init; } = [];
}

/// <summary>
/// Feature 运行时 schema 缓存模型。
/// </summary>
[CacheCatalog("Feature 运行时 Schema", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 33)]
[CacheKey("harbor:admin:access:runtime", Key = "feature-schema:{FeatureCode}", ExpirationSeconds = 600)]
[CacheTag(AdminAccessCacheKeys.RuntimeTag, typeof(AdminFeature), typeof(AdminFeatureField), typeof(AdminFeatureAction), typeof(AdminFeatureApi))]
public sealed class FeatureRuntimeSchemaCacheModel
{
    /// <summary>
    /// 功能编码。
    /// </summary>
    [CacheKeyPart]
    public string FeatureCode { get; init; } = string.Empty;

    /// <summary>
    /// 名称国际化 Key。
    /// </summary>
    public string NameKey { get; init; } = string.Empty;

    /// <summary>
    /// 名称兜底文本。
    /// </summary>
    public string? NameFallback { get; init; }

    /// <summary>
    /// 功能类型。
    /// </summary>
    public string FeatureType { get; init; } = string.Empty;

    /// <summary>
    /// 前端组件。
    /// </summary>
    public string? Component { get; init; }

    /// <summary>
    /// 路由路径。
    /// </summary>
    public string? RoutePath { get; init; }

    /// <summary>
    /// Schema 版本号。
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// 字段定义。
    /// </summary>
    public FeatureFieldCacheItem[] Fields { get; init; } = [];

    /// <summary>
    /// 动作定义。
    /// </summary>
    public FeatureActionCacheItem[] Actions { get; init; } = [];

    /// <summary>
    /// API 定义。
    /// </summary>
    public FeatureApiCacheItem[] Apis { get; init; } = [];
}

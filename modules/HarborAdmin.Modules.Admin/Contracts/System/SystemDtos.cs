namespace HarborAdmin.Modules.Admin.Contracts.System;

/// <summary>
/// 系统部门。
/// </summary>
public sealed record SystemDeptDto(
    string Id,
    string? Pid,
    string Name,
    string? Remark,
    int Status,
    IReadOnlyList<SystemDeptDto>? Children = null);

/// <summary>
/// 保存部门请求。
/// </summary>
public sealed record SaveSystemDeptRequest(
    string? Pid,
    string Name,
    string? Remark,
    int Status);

/// <summary>
/// 系统菜单元数据。
/// </summary>
public sealed record SystemMenuMetaDto(
    string? Title,
    string? Icon = null,
    string? ActiveIcon = null,
    string? ActivePath = null,
    int? Order = null,
    bool? AffixTab = null,
    int? AffixTabOrder = null,
    bool? HideInMenu = null,
    bool? HideInTab = null,
    bool? HideInBreadcrumb = null,
    bool? KeepAlive = null,
    bool? HideChildrenInMenu = null,
    string? Badge = null,
    string? BadgeType = null,
    string? BadgeVariants = null,
    string? Link = null,
    string? IframeSrc = null,
    int? MaxNumOfOpenTab = null,
    bool? NoBasicLayout = null,
    bool? OpenInNewWindow = null,
    IReadOnlyDictionary<string, object?>? Query = null,
    string? FeatureCode = null);

/// <summary>
/// 系统菜单。
/// </summary>
public sealed record SystemMenuDto(
    string Id,
    string? Pid,
    string Name,
    string Path,
    string Type,
    string? FeatureCode,
    string? Component,
    string? AuthCode,
    int Status,
    SystemMenuMetaDto? Meta,
    string? Redirect = null,
    IReadOnlyList<SystemMenuDto>? Children = null);

/// <summary>
/// 保存菜单请求。
/// </summary>
public sealed record SaveSystemMenuRequest(
    string? Pid,
    string Name,
    string? Path,
    string Type,
    string? FeatureCode,
    string? Component,
    string? AuthCode,
    int Status,
    SystemMenuMetaDto? Meta,
    string? Redirect = null,
    string? ActivePath = null,
    string? LinkSrc = null);

/// <summary>
/// 菜单同级排序请求。
/// </summary>
public sealed record ReorderSystemMenuRequest(
    string? Pid,
    IReadOnlyList<string> OrderedIds);

/// <summary>
/// 系统角色。
/// </summary>
public sealed record SystemRoleDto(
    string Id,
    string Name,
    string RoleCode,
    IReadOnlyList<string> MenuIds,
    IReadOnlyList<string> PermissionCodes,
    IReadOnlyList<SystemRoleFieldPolicyDto> FieldPolicies,
    IReadOnlyList<string> Permissions,
    string? Remark,
    int Status,
    string DataScopeType,
    string CreateTime);

/// <summary>
/// 保存角色请求。
/// </summary>
public sealed record SaveSystemRoleRequest(
    string Name,
    string? RoleCode,
    IReadOnlyList<string>? MenuIds,
    IReadOnlyList<string>? PermissionCodes,
    IReadOnlyList<SystemRoleFieldPolicyDto>? FieldPolicies,
    IReadOnlyList<string>? Permissions,
    string? Remark,
    int Status,
    string? DataScopeType);

/// <summary>
/// 系统角色字段策略。
/// </summary>
public sealed record SystemRoleFieldPolicyDto(
    string FeatureCode,
    string FieldName,
    bool Visible,
    bool Editable,
    bool Exportable,
    bool Masked);

/// <summary>
/// 系统用户。
/// </summary>
public sealed record SystemUserDto(
    string Id,
    string Name,
    string UserName,
    string? DeptId,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> RoleIds,
    string? Remark,
    int Status,
    string CreateTime);

/// <summary>
/// 保存用户请求。
/// </summary>
public sealed record SaveSystemUserRequest(
    string Name,
    string? UserName,
    string? Password,
    string? DeptId,
    IReadOnlyList<string>? Permissions,
    IReadOnlyList<string>? RoleIds,
    string? Remark,
    int Status);

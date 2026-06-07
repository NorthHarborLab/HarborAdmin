namespace HarborAdmin.Modules.Admin.Contracts.System;

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

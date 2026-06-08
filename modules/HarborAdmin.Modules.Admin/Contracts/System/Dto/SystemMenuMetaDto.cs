namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

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

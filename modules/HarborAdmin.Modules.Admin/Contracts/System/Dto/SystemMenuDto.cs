namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

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
    string? Title,
    string? Icon,
    string? ActiveIcon,
    string? ActivePath,
    int Order,
    bool AffixTab,
    int? AffixTabOrder,
    bool HideInMenu,
    bool HideInTab,
    bool KeepAlive,
    bool HideChildrenInMenu,
    string? Link,
    string? IframeSrc,
    bool OpenInNewWindow,
    string? MetaJson,
    SystemMenuMetaDto? Meta,
    string? Redirect = null,
    IReadOnlyList<SystemMenuDto>? Children = null);

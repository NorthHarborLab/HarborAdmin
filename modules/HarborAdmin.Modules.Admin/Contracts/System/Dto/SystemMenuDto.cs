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
    SystemMenuMetaDto? Meta,
    string? Redirect = null,
    IReadOnlyList<SystemMenuDto>? Children = null);

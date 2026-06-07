namespace HarborAdmin.Modules.Admin.Contracts.Access.Dto;

/// <summary>
/// 后端路由
/// </summary>
public sealed record BackendRouteDto(
    string Path,
    string Name,
    string Component,
    BackendRouteMetaDto Meta,
    string? Redirect = null,
    IReadOnlyList<BackendRouteDto>? Children = null);

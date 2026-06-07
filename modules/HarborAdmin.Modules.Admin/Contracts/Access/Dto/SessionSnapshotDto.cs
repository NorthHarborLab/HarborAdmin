namespace HarborAdmin.Modules.Admin.Contracts.Access.Dto;

/// <summary>
/// 会话访问包
/// </summary>
public sealed record SessionSnapshotDto(
    long SessionVersion,
    CurrentUserDto User,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<BackendRouteDto> Routes,
    IReadOnlyList<FieldPolicyDto> FieldPolicies,
    IReadOnlyList<DataScopeDto> DataScopes,
    string HomePath);

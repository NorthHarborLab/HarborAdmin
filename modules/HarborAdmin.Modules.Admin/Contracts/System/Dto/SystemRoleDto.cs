namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

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

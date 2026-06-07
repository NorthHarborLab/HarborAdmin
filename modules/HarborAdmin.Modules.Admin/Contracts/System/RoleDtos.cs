namespace HarborAdmin.Modules.Admin.Contracts.System;

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

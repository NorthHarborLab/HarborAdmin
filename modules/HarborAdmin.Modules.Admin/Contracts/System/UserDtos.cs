namespace HarborAdmin.Modules.Admin.Contracts.System;

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

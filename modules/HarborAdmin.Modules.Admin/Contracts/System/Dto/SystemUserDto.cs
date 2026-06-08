namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

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
    string CreateTime,
    bool IsSuperAdmin);

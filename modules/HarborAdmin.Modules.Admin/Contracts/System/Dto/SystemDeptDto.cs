namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

/// <summary>
/// 系统部门。
/// </summary>
public sealed record SystemDeptDto(
    string Id,
    string? Pid,
    string Name,
    string? Remark,
    int Status,
    IReadOnlyList<SystemDeptDto>? Children = null);

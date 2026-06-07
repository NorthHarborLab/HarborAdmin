namespace HarborAdmin.Modules.Admin.Contracts.System;

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

/// <summary>
/// 保存部门请求。
/// </summary>
public sealed record SaveSystemDeptRequest(
    string? Pid,
    string Name,
    string? Remark,
    int Status);

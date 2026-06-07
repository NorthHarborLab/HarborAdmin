namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// 数据范围
/// </summary>
public sealed record DataScopeDto(
    string RoleCode,
    string ScopeType,
    string? ScopeValueType,
    string? ScopeValueId);

namespace HarborAdmin.Modules.Admin.Contracts.Access.Dto;

/// <summary>
/// 当前用户信息
/// </summary>
public sealed record CurrentUserDto(string UserId, string Username, string RealName, string Avatar, string Desc, string HomePath, IReadOnlyList<string> Roles);
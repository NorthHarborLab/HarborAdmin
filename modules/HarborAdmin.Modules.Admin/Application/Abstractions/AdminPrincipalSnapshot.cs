namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// 从 access token 解析出的当前用户快照。
/// </summary>
public sealed record AdminPrincipalSnapshot(
    long Id,
    string UserName,
    string DisplayName);

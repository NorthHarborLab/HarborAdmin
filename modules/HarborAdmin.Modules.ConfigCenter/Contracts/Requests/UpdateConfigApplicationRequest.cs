namespace HarborAdmin.Modules.ConfigCenter.Contracts.Requests;

/// <summary>
/// 更新应用请求。
/// </summary>
public sealed record UpdateConfigApplicationRequest(string Name, string? Description);

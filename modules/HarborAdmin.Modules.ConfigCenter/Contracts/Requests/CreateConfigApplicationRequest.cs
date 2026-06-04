namespace HarborAdmin.Modules.ConfigCenter.Contracts.Requests;

/// <summary>
/// 创建应用请求。
/// </summary>
public sealed record CreateConfigApplicationRequest(string AppId, string Name, string? Description);

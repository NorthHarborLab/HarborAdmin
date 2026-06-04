namespace HarborAdmin.Modules.ConfigCenter.Contracts.Requests;

/// <summary>
/// 更新配置项请求。
/// </summary>
public sealed record UpdateConfigItemRequest(
    string Group,
    string Key,
    string Value,
    string ValueType,
    string? Remark);

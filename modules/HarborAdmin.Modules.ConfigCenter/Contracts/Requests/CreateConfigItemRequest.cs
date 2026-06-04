namespace HarborAdmin.Modules.ConfigCenter.Contracts.Requests;

/// <summary>
/// 创建配置项请求。
/// </summary>
public sealed record CreateConfigItemRequest(
    string Group,
    string Key,
    string Value,
    string ValueType,
    string? Remark);

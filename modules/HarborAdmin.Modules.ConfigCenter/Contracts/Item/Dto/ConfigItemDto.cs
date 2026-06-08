namespace HarborAdmin.Modules.ConfigCenter.Contracts.Item.Dto;

/// <summary>
/// 草稿配置项 DTO。
/// </summary>
public sealed record ConfigItemDto(
    long Id,
    string AppId,
    string Group,
    string Key,
    string Value,
    string ValueType,
    string? Remark,
    DateTimeOffset UpdatedAt);

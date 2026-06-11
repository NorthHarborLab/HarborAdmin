namespace HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

/// <summary>
/// 发布快照配置项 DTO，供管理端按分组展示。
/// </summary>
public sealed record ConfigReleaseItemDto(
    string Group,
    string Key,
    string Value,
    string ValueType);

namespace HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

/// <summary>
/// 管理端已发布配置快照（含分组配置项，供树形展示）。
/// </summary>
public sealed record AdminPublishedConfigSnapshot(
    int Version,
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyList<ConfigReleaseItemDto> Items);

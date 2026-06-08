namespace HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

/// <summary>
/// 已发布配置快照，供 TCP 客户端拉取。
/// </summary>
public sealed record PublishedConfigSnapshot(int Version, IReadOnlyDictionary<string, string> Data);

namespace HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

/// <summary>
/// 发布记录 DTO。
/// </summary>
public sealed record ConfigReleaseDto(
    long Id,
    string AppId,
    int Version,
    string? PublishedBy,
    DateTimeOffset PublishedAt);

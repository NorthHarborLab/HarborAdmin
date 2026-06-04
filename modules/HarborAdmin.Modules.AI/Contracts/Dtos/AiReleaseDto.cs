namespace HarborAdmin.Modules.AI.Contracts.Dtos;

/// <summary>
/// AI 配置发布 DTO。
/// </summary>
public sealed record AiReleaseDto(
    long Id,
    int Version,
    string Checksum,
    string? PublishedBy,
    string? Remark,
    bool Active,
    DateTimeOffset PublishedAt);

/// <summary>
/// AI 已发布快照 DTO。
/// </summary>
public sealed record AiPublishedSnapshotDto(
    long ReleaseId,
    int Version,
    string Checksum,
    string SnapshotJson,
    DateTimeOffset PublishedAt);

/// <summary>
/// 发布 AI 配置请求。
/// </summary>
public sealed record PublishAiConfigRequest(string? PublishedBy, string? Remark);

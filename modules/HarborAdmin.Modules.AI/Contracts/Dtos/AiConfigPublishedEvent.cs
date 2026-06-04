namespace HarborAdmin.Modules.AI.Contracts.Dtos;

/// <summary>
/// AI 配置发布事件。
/// </summary>
public sealed record AiConfigPublishedEvent(long ReleaseId, int Version, string Checksum);

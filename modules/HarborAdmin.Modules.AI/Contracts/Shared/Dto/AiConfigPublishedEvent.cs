namespace HarborAdmin.Modules.AI.Contracts.Shared.Dto;

/// <summary>
/// AI 配置发布事件。
/// </summary>
public sealed record AiConfigPublishedEvent(long ReleaseId, int Version, string Checksum);

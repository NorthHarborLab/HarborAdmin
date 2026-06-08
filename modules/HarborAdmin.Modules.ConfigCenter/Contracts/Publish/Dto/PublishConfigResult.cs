namespace HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

/// <summary>
/// 发布操作结果。
/// </summary>
public sealed record PublishConfigResult(long ReleaseId, int Version);

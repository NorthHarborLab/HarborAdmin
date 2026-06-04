namespace HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;

/// <summary>
/// 发布操作结果。
/// </summary>
public sealed record PublishConfigResult(long ReleaseId, int Version);

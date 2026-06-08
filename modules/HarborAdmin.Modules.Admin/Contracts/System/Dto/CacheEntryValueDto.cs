namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

/// <summary>
/// 缓存条目内容。
/// </summary>
public sealed record CacheEntryValueDto(
    string Key,
    bool Found,
    string? ModelTypeName,
    string? Json,
    int SizeBytes,
    bool Truncated);

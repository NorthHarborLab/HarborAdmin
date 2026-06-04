namespace HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;

/// <summary>
/// 应用信息 DTO。
/// </summary>
public sealed record ConfigApplicationDto(
    long Id,
    string AppId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt);

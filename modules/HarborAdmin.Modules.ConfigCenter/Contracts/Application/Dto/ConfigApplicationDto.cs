namespace HarborAdmin.Modules.ConfigCenter.Contracts.Application.Dto;

/// <summary>
/// 应用信息 DTO。
/// </summary>
public sealed record ConfigApplicationDto(
    long Id,
    string AppId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt);
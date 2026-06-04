namespace HarborAdmin.Modules.International.Contracts.Dtos;

/// <summary>
/// 国际化页面 DTO。
/// </summary>
public sealed record InternationalPageDto(
    long Id,
    string PageKey,
    int Version,
    string Name,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

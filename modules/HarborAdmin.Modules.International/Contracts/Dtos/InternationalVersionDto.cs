namespace HarborAdmin.Modules.International.Contracts.Dtos;

/// <summary>
/// 国际化资源版本 DTO。
/// </summary>
public sealed record InternationalVersionDto(int Version, IReadOnlyList<InternationalPageVersionDto> Pages);

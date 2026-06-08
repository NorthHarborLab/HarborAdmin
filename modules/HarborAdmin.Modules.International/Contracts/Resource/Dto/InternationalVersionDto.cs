using HarborAdmin.Modules.International.Contracts.Page.Dto;

namespace HarborAdmin.Modules.International.Contracts.Resource.Dto;

/// <summary>
/// 国际化资源版本 DTO。
/// </summary>
public sealed record InternationalVersionDto(int Version, IReadOnlyList<InternationalPageVersionDto> Pages);

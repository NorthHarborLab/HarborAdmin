using HarborAdmin.Modules.International.Contracts.Dtos;

namespace HarborAdmin.Modules.International.Contracts.Requests;

/// <summary>
/// 更新国际化树节点请求。
/// </summary>
public sealed record UpdateInternationalEntryRequest(
    string Key,
    string? Remark,
    int SortOrder,
    IReadOnlyList<InternationalEntryTranslationDto> Translations);

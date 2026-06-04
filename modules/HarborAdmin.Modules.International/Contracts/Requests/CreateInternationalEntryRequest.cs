using HarborAdmin.Modules.International.Contracts.Dtos;

namespace HarborAdmin.Modules.International.Contracts.Requests;

/// <summary>
/// 创建国际化树节点请求。
/// </summary>
public sealed record CreateInternationalEntryRequest(
    long? ParentId,
    string Key,
    string? Remark,
    int SortOrder,
    IReadOnlyList<InternationalEntryTranslationDto> Translations);

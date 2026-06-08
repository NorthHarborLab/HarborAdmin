namespace HarborAdmin.Modules.International.Contracts.Entry.Dto;

/// <summary>
/// 国际化条目 DTO。
/// </summary>
public sealed record InternationalEntryDto(
    long Id,
    long PageId,
    long? ParentId,
    string Key,
    string? DefaultValue,
    string? Remark,
    int SortOrder,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<InternationalEntryTranslationDto> Translations,
    IReadOnlyList<InternationalEntryDto> Children);

namespace HarborAdmin.Modules.International.Contracts;

/// <summary>
/// 国际化页面 DTO
/// </summary>
public sealed record InternationalPageDto(
    long Id,
    string PageKey,
    int Version,
    string Name,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// 创建国际化页面请求
/// </summary>
public sealed record CreateInternationalPageRequest(
    string PageKey,
    string Name,
    string? Remark);

/// <summary>
/// 更新国际化页面请求
/// </summary>
public sealed record UpdateInternationalPageRequest(string PageKey, string Name, string? Remark);

/// <summary>
/// 国际化条目 DTO
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

/// <summary>
/// 国际化条目翻译 DTO
/// </summary>
public sealed record InternationalEntryTranslationDto(string Locale, string Value);

/// <summary>
/// 创建国际化树节点请求
/// </summary>
public sealed record CreateInternationalEntryRequest(
    long? ParentId,
    string Key,
    string? Remark,
    int SortOrder,
    IReadOnlyList<InternationalEntryTranslationDto> Translations);

/// <summary>
/// 更新国际化树节点请求
/// </summary>
public sealed record UpdateInternationalEntryRequest(
    string Key,
    string? Remark,
    int SortOrder,
    IReadOnlyList<InternationalEntryTranslationDto> Translations);

/// <summary>
/// 国际化资源版本 DTO
/// </summary>
public sealed record InternationalVersionDto(int Version, IReadOnlyList<InternationalPageVersionDto> Pages);

/// <summary>
/// 国际化页面资源版本 DTO
/// </summary>
public sealed record InternationalPageVersionDto(string PageKey, int Version);

/// <summary>
/// 前端国际化资源包
/// </summary>
public sealed record InternationalBundleDto(int Version, IReadOnlyDictionary<string, object> Messages);

/// <summary>
/// 前端单页面国际化资源包
/// </summary>
public sealed record InternationalPageBundleDto(
    string PageKey,
    int Version,
    IReadOnlyDictionary<string, object> Messages);

namespace HarborAdmin.Modules.Admin.Contracts.Dictionary.Dto;

/// <summary>
/// Admin 字典类型。
/// </summary>
public sealed record AdminDictionaryDto(
    long Id,
    string DictCode,
    string Name,
    string? Remark,
    int SortOrder,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

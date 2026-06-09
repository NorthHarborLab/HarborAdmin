using System.Text.Json;

namespace HarborAdmin.Modules.Admin.Contracts.Dictionary.Dto;

/// <summary>
/// Admin 字典项。
/// </summary>
public sealed record AdminDictionaryItemDto(
    long Id,
    string DictCode,
    string ItemValue,
    string ItemLabel,
    string? Color,
    string? Remark,
    int SortOrder,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// 前端字典选项。
/// </summary>
public sealed record AdminDictionaryOptionDto(
    string Label,
    JsonElement Value,
    string? Color,
    bool Disabled);

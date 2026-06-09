using System.Text.Json;
using HarborAdmin.Modules.Admin.Contracts.Dictionary.Dto;

namespace HarborAdmin.Modules.Admin.Application.Services.Dictionary;

/// <summary>
/// Admin 内置字典兜底。
/// </summary>
internal static class AdminDictionaryBuiltIns
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly BuiltInDictionary[] Dictionaries =
    [
        new(
            "common.enabledStatus",
            "启用状态",
            10,
            [
                new("启用", 1, "success", false),
                new("禁用", 0, "error", false),
            ]),
        new(
            "system.dataScopeType",
            "数据范围",
            20,
            [
                new("全部数据", "All", "success", false),
                new("本部门", "Dept", "processing", false),
                new("本部门及下级", "DeptWithChildren", "processing", false),
                new("仅本人", "Self", "default", false),
                new("本人及下级", "SelfWithSubordinates", "default", false),
                new("自定义部门", "CustomDept", "warning", false),
                new("自定义用户", "CustomUser", "warning", false),
            ]),
    ];

    /// <summary>
    /// 查询内置字典类型。
    /// </summary>
    public static IReadOnlyList<AdminDictionaryDto> ListDictionaries(string? keyword)
    {
        var normalized = keyword?.Trim();
        return Dictionaries
            .Where(item => string.IsNullOrWhiteSpace(normalized)
                           || item.DictCode.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                           || item.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .Select(item => new AdminDictionaryDto(
                0,
                item.DictCode,
                item.Name,
                "系统内置字典",
                item.SortOrder,
                true,
                DateTimeOffset.MinValue,
                DateTimeOffset.MinValue))
            .ToArray();
    }

    /// <summary>
    /// 获取内置字典种子数据。
    /// </summary>
    public static IReadOnlyList<BuiltInDictionary> ListSeeds() => Dictionaries;

    /// <summary>
    /// 获取内置字典选项。
    /// </summary>
    public static IReadOnlyList<AdminDictionaryOptionDto>? GetOptions(string dictCode)
    {
        var dictionary = Dictionaries.FirstOrDefault(
            item => string.Equals(item.DictCode, dictCode, StringComparison.OrdinalIgnoreCase));
        return dictionary?.Items
            .Select(item => new AdminDictionaryOptionDto(
                item.Label,
                JsonSerializer.SerializeToElement(item.Value, JsonOptions),
                item.Color,
                item.Disabled))
            .ToArray();
    }

    internal sealed record BuiltInDictionary(
        string DictCode,
        string Name,
        int SortOrder,
        IReadOnlyList<BuiltInDictionaryItem> Items);

    internal sealed record BuiltInDictionaryItem(
        string Label,
        object Value,
        string? Color,
        bool Disabled);
}

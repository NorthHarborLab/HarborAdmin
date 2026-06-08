using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.Modules.AI.Application.Services.Shared;

/// <summary>
/// AI 模块输入规范化工具。
/// </summary>
internal static class AiNormalizationHelper
{
    /// <summary>
    /// 规范化标识键。
    /// </summary>
    public static string NormalizeKey(string value, string name)
    {
        var normalized = NormalizeRequired(value, name);
        if (normalized.Contains(' ') || normalized.Contains('/'))
        {
            throw new ValidationDomainException($"{name} cannot contain spaces or '/'.", errorMeta: new { Field = name });
        }

        return normalized;
    }

    /// <summary>
    /// 规范化必填字符串。
    /// </summary>
    public static string NormalizeRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationDomainException($"{name} is required.", errorMeta: new { Field = name });
        }

        return value.Trim();
    }

    /// <summary>
    /// 规范化可选字符串。
    /// </summary>
    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 规范化逗号分隔列表。
    /// </summary>
    public static string? NormalizeCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return items.Length == 0 ? null : string.Join(',', items);
    }
}

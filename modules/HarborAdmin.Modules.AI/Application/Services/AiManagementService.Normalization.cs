using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    private static string NormalizeKey(string value, string name)
    {
        var normalized = NormalizeRequired(value, name);
        if (normalized.Contains(' ') || normalized.Contains('/'))
        {
            throw new ValidationDomainException($"{name} cannot contain spaces or '/'.", errorMeta: new { Field = name });
        }

        return normalized;
    }

    private static string NormalizeRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationDomainException($"{name} is required.", errorMeta: new { Field = name });
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCsv(string? value)
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

using System.Reflection;
using System.Text.RegularExpressions;

namespace HarborAdmin.BuildingBlocks.Abstractions.Results;

/// <summary>
/// 错误码发现与治理校验。
/// </summary>
public static partial class HarborErrorCatalog
{
    /// <summary>
    /// 从程序集发现并校验错误定义。
    /// </summary>
    public static IReadOnlyList<HarborErrorDefinition> Discover(IEnumerable<Assembly> assemblies)
    {
        var definitions = assemblies
            .Distinct()
            .SelectMany(GetLoadableTypes)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.FieldType == typeof(HarborErrorDefinition))
            .Select(field => (HarborErrorDefinition?)field.GetValue(null))
            .Where(definition => definition is not null)
            .Cast<HarborErrorDefinition>()
            .OrderBy(definition => definition.Code, StringComparer.Ordinal)
            .ToArray();

        Validate(definitions);
        return definitions;
    }

    /// <summary>
    /// 校验错误定义集合。
    /// </summary>
    public static void Validate(IReadOnlyList<HarborErrorDefinition> definitions)
    {
        var duplicate = definitions
            .GroupBy(definition => definition.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate error code '{duplicate.Key}'.");
        }

        foreach (var definition in definitions)
        {
            if (!ErrorCodePattern().IsMatch(definition.Code))
            {
                throw new InvalidOperationException($"Invalid error code '{definition.Code}'.");
            }

            var prefix = definition.Code.Split('.', 2)[0];
            if (!string.Equals(prefix, definition.Module, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Error code '{definition.Code}' does not match module '{definition.Module}'.");
            }

            if (string.IsNullOrWhiteSpace(definition.DefaultMessage))
            {
                throw new InvalidOperationException($"Error code '{definition.Code}' has no default message.");
            }

            if (string.IsNullOrWhiteSpace(definition.Since))
            {
                throw new InvalidOperationException($"Error code '{definition.Code}' has no introduction version.");
            }

            var arguments = definition.ArgumentNames ?? [];
            if (arguments.Any(string.IsNullOrWhiteSpace) || arguments.Distinct(StringComparer.Ordinal).Count() != arguments.Count)
            {
                throw new InvalidOperationException($"Error code '{definition.Code}' has invalid argument names.");
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    [GeneratedRegex("^[A-Z][A-Z0-9]*(?:\\.[A-Z][A-Z0-9_]*){2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex ErrorCodePattern();
}

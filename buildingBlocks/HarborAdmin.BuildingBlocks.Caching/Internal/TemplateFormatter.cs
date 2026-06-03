using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HarborAdmin.BuildingBlocks.Caching.Internal;

/// <summary>
/// 缓存 key 与 tag 模板格式化工具。
/// 统一处理 {PropertyName} 模板替换，供 key、tag、Redis 结构 key 共同复用。
/// </summary>
internal static partial class TemplateFormatter
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> PropertyCache = new();

    /// <summary>
    /// 使用对象属性值格式化模板。
    /// </summary>
    public static string Format(string template, object source)
    {
        var properties = GetProperties(source.GetType());
        return TokenRegex().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            if (!properties.TryGetValue(name, out var property))
            {
                throw new InvalidOperationException($"Template '{template}' references unknown property '{name}'.");
            }

            // 使用 InvariantCulture，避免数字/日期在不同区域设置下生成不同缓存 key。
            var value = property.GetValue(source);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        });
    }

    /// <summary>
    /// 使用键值字典格式化模板。
    /// </summary>
    public static string Format(string template, IReadOnlyDictionary<string, object?> values)
    {
        return TokenRegex().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            if (!values.TryGetValue(name, out var value))
            {
                throw new InvalidOperationException($"Template '{template}' references unknown key part '{name}'.");
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        });
    }

    /// <summary>
    /// 获取可参与模板替换的公开实例属性。
    /// </summary>
    public static IReadOnlyDictionary<string, PropertyInfo> GetProperties(Type type) =>
        // 排除索引器属性；模板替换只能安全地读取无参数实例属性。
        PropertyCache.GetOrAdd(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// 获取模板占位符正则。
    /// </summary>
    [GeneratedRegex(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex TokenRegex();
}
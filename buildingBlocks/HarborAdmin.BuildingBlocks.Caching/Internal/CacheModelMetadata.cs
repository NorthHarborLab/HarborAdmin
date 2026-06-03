using System.Collections.Concurrent;
using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.BuildingBlocks.Caching.Internal;

/// <summary>
/// 强类型缓存模型元数据。
/// 缓存模型的反射元数据。这里集中解析 Attribute，避免每次 Get/Set 都重复扫描类型。
/// </summary>
internal sealed class CacheModelMetadata
{
    private static readonly ConcurrentDictionary<Type, CacheModelMetadata> Cache = new();

    public required Type ModelType { get; init; }

    public required string Prefix { get; init; }

    public required string KeyTemplate { get; init; }

    public required TimeSpan? Expiration { get; init; }

    public required IReadOnlyList<string> ClassTagTemplates { get; init; }

    public required IReadOnlyList<string> PropertyTagTemplates { get; init; }

    /// <summary>
    /// 获取指定缓存模型类型的元数据。
    /// </summary>
    public static CacheModelMetadata For(Type modelType) =>
        Cache.GetOrAdd(modelType, Create);

    /// <summary>
    /// 根据 key 字段值构建完整缓存 key。
    /// </summary>
    public string BuildKey(IReadOnlyDictionary<string, object?> keyParts)
    {
        // CacheKeyAttribute.Prefix 只负责稳定命名空间，KeyTemplate 负责模型实例的唯一后缀。
        var suffix = TemplateFormatter.Format(KeyTemplate, keyParts);
        return string.IsNullOrWhiteSpace(suffix) ? Prefix : $"{Prefix}:{suffix}";
    }

    /// <summary>
    /// 根据缓存模型实例构建需要绑定的 tag 列表。
    /// </summary>
    public IReadOnlyList<string> BuildTags(object model)
    {
        // 同一个模型可能由类级 Tag 与属性级 Tag 同时命中，使用 HashSet 去重。
        var tags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var template in ClassTagTemplates)
        {
            tags.Add(TemplateFormatter.Format(template, model));
        }

        foreach (var template in PropertyTagTemplates)
        {
            tags.Add(TemplateFormatter.Format(template, model));
        }

        return tags.ToArray();
    }

    /// <summary>
    /// 从缓存模型类型创建元数据。
    /// </summary>
    private static CacheModelMetadata Create(Type modelType)
    {
        // 强类型缓存必须显式声明 [CacheKey]，否则无法保证 key 的可预测性。
        var key = modelType.GetCustomAttributes(typeof(CacheKeyAttribute), false)
                      .Cast<CacheKeyAttribute>()
                      .FirstOrDefault()
                  ?? throw new InvalidOperationException($"Cache model '{modelType.FullName}' must declare [CacheKey].");

        var properties = TemplateFormatter.GetProperties(modelType);
        foreach (var token in ExtractTokens(key.Key))
        {
            // key 模板里的每个占位符都必须对应 [CacheKeyPart]，避免误把普通字段加入 key。
            if (!properties.TryGetValue(token, out var property) ||
                property.GetCustomAttributes(typeof(CacheKeyPartAttribute), true).Length == 0)
            {
                throw new InvalidOperationException($"Cache model '{modelType.FullName}' key token '{token}' must map to a [CacheKeyPart] property.");
            }
        }

        var propertyTags = properties.Values
            // 属性级 Tag 允许缓存模型把某个字段映射为失效维度，例如 AppId、Locale。
            .SelectMany(property => property.GetCustomAttributes(typeof(CacheTagPartAttribute), true)
                .Cast<CacheTagPartAttribute>()
                .Select(attribute => attribute.Template))
            .ToArray();

        return new CacheModelMetadata
        {
            ModelType = modelType,
            Prefix = key.Prefix.TrimEnd(':'),
            KeyTemplate = key.Key,
            Expiration = key.ExpirationSeconds > 0 ? TimeSpan.FromSeconds(key.ExpirationSeconds) : null,
            ClassTagTemplates = modelType.GetCustomAttributes(typeof(CacheTagAttribute), false)
                .Cast<CacheTagAttribute>()
                .Select(attribute => attribute.Template)
                .ToArray(),
            PropertyTagTemplates = propertyTags
        };
    }

    /// <summary>
    /// 提取模板中的占位符名称。
    /// </summary>
    private static IEnumerable<string> ExtractTokens(string template)
    {
        // 只识别 {Name} 形式的简单标识符，和 TemplateFormatter 的替换规则保持一致。
        var matches = System.Text.RegularExpressions.Regex.Matches(template, @"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}");
        return matches.Select(match => match.Groups["name"].Value);
    }
}
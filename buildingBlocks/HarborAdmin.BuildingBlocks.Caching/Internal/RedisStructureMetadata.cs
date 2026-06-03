using System.Collections.Concurrent;
using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.BuildingBlocks.Caching.Internal;

/// <summary>
/// Redis 强类型结构元数据。
/// Redis Hash/List/Counter 的 key 模板元数据，和对象缓存模型分开维护。
/// </summary>
internal sealed class RedisStructureMetadata
{
    private static readonly ConcurrentDictionary<(Type Type, RedisStructureKind Kind), RedisStructureMetadata> Cache = new();

    public required string KeyTemplate { get; init; }

    /// <summary>
    /// 获取指定模型和 Redis 结构类型的元数据。
    /// </summary>
    public static RedisStructureMetadata For(Type type, RedisStructureKind kind) =>
        Cache.GetOrAdd((type, kind), static entry => Create(entry.Type, entry.Kind));

    /// <summary>
    /// 根据模型实例构建 Redis key。
    /// </summary>
    public string BuildKey(object model) => TemplateFormatter.Format(KeyTemplate, model);

    /// <summary>
    /// 从模型类型和结构类型创建元数据。
    /// </summary>
    private static RedisStructureMetadata Create(Type type, RedisStructureKind kind)
    {
        // 同一个模型可以分别声明 Hash/List/Counter key，因此按结构类型选择对应 Attribute。
        var template = kind switch
        {
            RedisStructureKind.Hash => type.GetCustomAttributes(typeof(RedisHashAttribute), false)
                .Cast<RedisHashAttribute>()
                .FirstOrDefault()
                ?.KeyTemplate,
            RedisStructureKind.List => type.GetCustomAttributes(typeof(RedisListAttribute), false)
                .Cast<RedisListAttribute>()
                .FirstOrDefault()
                ?.KeyTemplate,
            RedisStructureKind.Counter => type.GetCustomAttributes(typeof(RedisCounterAttribute), false)
                .Cast<RedisCounterAttribute>()
                .FirstOrDefault()
                ?.KeyTemplate,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        if (string.IsNullOrWhiteSpace(template))
        {
            // 结构化 Redis API 必须显式建模 key，避免调用方临时拼接字符串导致 key 漂移。
            throw new InvalidOperationException($"Redis structure model '{type.FullName}' must declare the matching Redis structure attribute.");
        }

        return new RedisStructureMetadata { KeyTemplate = template };
    }
}

/// <summary>
/// Redis 强类型结构类别。
/// </summary>
internal enum RedisStructureKind
{
    Hash,
    List,
    Counter
}
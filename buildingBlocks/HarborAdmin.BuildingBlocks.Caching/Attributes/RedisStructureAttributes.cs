namespace HarborAdmin.BuildingBlocks.Caching.Attributes;

/// <summary>
/// Redis Hash 结构 key 模板。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RedisHashAttribute(string keyTemplate) : Attribute
{
    /// <summary>
    /// Redis key 模板。
    /// </summary>
    public string KeyTemplate { get; } = keyTemplate;
}

/// <summary>
/// Redis List 结构 key 模板。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RedisListAttribute(string keyTemplate) : Attribute
{
    /// <summary>
    /// Redis key 模板。
    /// </summary>
    public string KeyTemplate { get; } = keyTemplate;
}

/// <summary>
/// Redis Counter 结构 key 模板。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RedisCounterAttribute(string keyTemplate) : Attribute
{
    /// <summary>
    /// Redis key 模板。
    /// </summary>
    public string KeyTemplate { get; } = keyTemplate;
}

/// <summary>
/// 标记 Redis typed structure key 模板可使用的属性。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RedisKeyPartAttribute : Attribute;

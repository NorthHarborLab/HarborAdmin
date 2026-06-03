namespace HarborAdmin.BuildingBlocks.Caching.Attributes;

/// <summary>
/// 声明强类型缓存模型的 key 前缀、key 模板与默认过期时间。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheKeyAttribute(string prefix) : Attribute
{
    /// <summary>
    /// 缓存 key 前缀。
    /// </summary>
    public string Prefix { get; } = prefix;

    /// <summary>
    /// key 模板，例如 <c>{Id}</c> 或 <c>{AppId}:{Locale}</c>。
    /// </summary>
    public string Key { get; init; } = "{Id}";

    /// <summary>
    /// 默认过期秒数；为空时使用全局默认值。
    /// </summary>
    public int ExpirationSeconds { get; init; }
}

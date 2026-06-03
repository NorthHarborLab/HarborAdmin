namespace HarborAdmin.BuildingBlocks.Caching.Attributes;

/// <summary>
/// 声明属性值可生成的 tag 模板。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class CacheTagPartAttribute(string template) : Attribute
{
    /// <summary>
    /// tag 模板。
    /// </summary>
    public string Template { get; } = template;
}

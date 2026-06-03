namespace HarborAdmin.BuildingBlocks.Caching.Attributes;

/// <summary>
/// 声明强类型缓存模型写入时需要绑定的 tag 模板。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CacheTagAttribute : Attribute
{
    /// <summary>
    /// 创建 tag 声明。
    /// </summary>
    /// <param name="template">tag 模板。</param>
    /// <param name="invalidatesOn">触发该 tag 失效的实体类型。</param>
    public CacheTagAttribute(string template, params Type[] invalidatesOn)
    {
        Template = template;
        InvalidatesOn = invalidatesOn;
    }

    /// <summary>
    /// tag 模板。
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// 触发该 tag 失效的实体类型。
    /// </summary>
    public IReadOnlyList<Type> InvalidatesOn { get; } = [];
}

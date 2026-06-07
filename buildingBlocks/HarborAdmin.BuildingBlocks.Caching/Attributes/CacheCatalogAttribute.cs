namespace HarborAdmin.BuildingBlocks.Caching.Attributes;

/// <summary>
/// 缓存模型运维目录元数据。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheCatalogAttribute : Attribute
{
    /// <summary>
    /// 创建缓存目录元数据。
    /// </summary>
    /// <param name="displayName">展示名称。</param>
    public CacheCatalogAttribute(string displayName)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// 展示名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 所属模块。
    /// </summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>
    /// 排序值。
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// 描述。
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 分组 prefix；为空时使用模型 prefix。
    /// </summary>
    public string GroupPrefix { get; init; } = string.Empty;

    /// <summary>
    /// 分组展示名称；为空时使用首个模型的展示名称。
    /// </summary>
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    /// 是否支持批量清理；默认根据 tag 模板自动判断，显式设为 false 可覆盖。
    /// </summary>
    public bool SupportsBulkClear { get; init; } = true;

    /// <summary>
    /// 查看内容时需要脱敏的 JSON 属性名。
    /// </summary>
    public string[] SensitiveFields { get; init; } = [];
}

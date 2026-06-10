namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 模块元数据基类。
/// </summary>
public abstract class HarborModuleMetadataBase : IHarborModuleMetadata
{
    private readonly IReadOnlyDictionary<string, object?> _properties;

    /// <summary>
    /// 初始化模块元数据。
    /// </summary>
    /// <param name="properties">模块扩展属性。</param>
    protected HarborModuleMetadataBase(IReadOnlyDictionary<string, object?>? properties = null)
    {
        _properties = properties ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public abstract string ModuleName { get; }

    /// <inheritdoc />
    public abstract string GetDbKey();

    /// <inheritdoc />
    public bool TryGetProperty(string key, out object? value) =>
        _properties.TryGetValue(key, out value);
}

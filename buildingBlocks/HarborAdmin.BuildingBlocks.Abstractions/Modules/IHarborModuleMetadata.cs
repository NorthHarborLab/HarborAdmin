namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 模块元数据。
/// </summary>
public interface IHarborModuleMetadata
{
    /// <summary>
    /// 模块名称。
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// 获取模块默认数据库 Key。
    /// </summary>
    string GetDbKey();

    /// <summary>
    /// 尝试获取模块扩展属性。
    /// </summary>
    /// <param name="key">属性键。</param>
    /// <param name="value">属性值。</param>
    /// <returns>存在该属性时返回 <see langword="true"/>。</returns>
    bool TryGetProperty(string key, out object? value);
}

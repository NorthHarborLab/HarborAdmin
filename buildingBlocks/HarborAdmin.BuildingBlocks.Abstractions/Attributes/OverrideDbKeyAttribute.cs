namespace HarborAdmin.BuildingBlocks.Abstractions.Attributes;

/// <summary>
/// 覆盖模块默认数据库 Key。
/// </summary>
/// <param name="key">数据库 Key。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OverrideDbKeyAttribute(string key) : Attribute
{
    /// <summary>
    /// 数据库 Key。
    /// </summary>
    public string Key { get; } = key;
}

namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>
/// 声明实体所属的 FreeSql 数据库 Key。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DbKeyAttribute(string key) : Attribute
{
    /// <summary>
    /// FreeSqlCloud 数据库注册 Key。
    /// </summary>
    public string Key { get; } = key;
}

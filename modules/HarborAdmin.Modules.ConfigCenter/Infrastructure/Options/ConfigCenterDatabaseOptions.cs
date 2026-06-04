namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Options;

/// <summary>
/// ConfigCenter 数据库连接配置(FreeSql)
/// </summary>
public sealed class ConfigCenterDatabaseOptions
{
    /// <summary>
    /// 配置节名称:<c>ConfigCenter:Database</c>
    /// </summary>
    public const string SectionName = "ConfigCenter:Database";

    /// <summary>
    /// 数据库类型:<c>Sqlite</c>,<c>PostgreSQL</c>,<c>SqlServer</c> 等(不区分大小写)
    /// </summary>
    public string DataType { get; set; } = "Sqlite";

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=configcenter.db";
}

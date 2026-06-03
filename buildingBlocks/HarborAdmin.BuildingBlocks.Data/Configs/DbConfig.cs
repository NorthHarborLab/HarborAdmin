namespace HarborAdmin.BuildingBlocks.Data.Configs;

/// <summary>
/// FreeSql 数据库配置
/// </summary>
public sealed class DbConfig
{
    /// <summary>
    /// 配置节名称：<c>DbConfig</c>
    /// </summary>
    public const string SectionName = "DbConfig";

    /// <summary>
    /// 数据库配置列表。
    /// </summary>
    public DbConnectionConfig[] Databases { get; set; } = [];
}

/// <summary>
/// FreeSql 数据库连接配置
/// </summary>
public sealed class DbConnectionConfig
{
    /// <summary>
    /// FreeSqlCloud 注册键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 数据库类型：Sqlite、PostgreSQL、SqlServer 等（不区分大小写）
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 主库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 启动时是否 CodeFirst 同步表结构
    /// </summary>
    public bool SyncStructure { get; set; }

    /// <summary>
    /// 是否只读库。只读库不执行 CodeFirst 同步结构
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// 读写分离从库列表
    /// </summary>
    public SlaveDb[]? SlaveList { get; set; }
}

/// <summary>
/// 读写分离从库
/// </summary>
public sealed class SlaveDb
{
    /// <summary>
    /// 负载权重
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// 从库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}

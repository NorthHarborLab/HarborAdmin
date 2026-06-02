namespace HarborAdmin.BuildingBlocks.Data.Configs;

/// <summary>
/// FreeSql 数据库配置根
/// </summary>
public sealed class DbConfig
{
    /// <summary>
    /// 配置节名称：<c>DbConfig</c>
    /// </summary>
    public const string SectionName = "DbConfig";

    /// <summary>
    /// 旧版单库配置未显式指定 Key 时使用的默认库键。
    /// </summary>
    public const string DefaultDbKey = "DefaultDb";

    /// <summary>
    /// 数据库配置列表。存在时优先使用该列表；否则兼容旧版单库字段
    /// </summary>
    public DbConnectionConfig[]? Databases { get; set; }

    /// <summary>
    /// FreeSqlCloud 注册键
    /// </summary>
    public string Key { get; set; } = DefaultDbKey;

    /// <summary>
    /// 数据库类型：Sqlite、PostgreSQL、MySql、SqlServer 等（不区分大小写）
    /// </summary>
    public string DataType { get; set; } = "Sqlite";

    /// <summary>
    /// 主库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=configcenter.db";

    /// <summary>
    /// 启动时是否 CodeFirst 同步表结构
    /// </summary>
    public bool SyncStructure { get; set; }

    /// <summary>
    /// 读写分离从库列表
    /// </summary>
    public SlaveDb[]? SlaveList { get; set; }

    /// <summary>
    /// 附加数据库（旧版多库配置，建议改用 <see cref="Databases"/>）
    /// </summary>
    public DbConnectionConfig[]? Dbs { get; set; }

    /// <summary>
    /// 归一化为数据库配置列表
    /// </summary>
    public IReadOnlyList<DbConnectionConfig> GetDatabases()
    {
        if (Databases is { Length: > 0 })
        {
            return Databases;
        }

        var databases = new List<DbConnectionConfig>
        {
            new()
            {
                Key = Key,
                DataType = DataType,
                ConnectionString = ConnectionString,
                SyncStructure = SyncStructure,
                ReadOnly = false,
                SlaveList = SlaveList,
            },
        };

        if (Dbs is { Length: > 0 })
        {
            databases.AddRange(Dbs);
        }

        return databases;
    }
}

/// <summary>
/// 单库 FreeSql 配置
/// </summary>
public sealed class DbConnectionConfig
{
    /// <summary>
    /// FreeSqlCloud 注册键
    /// </summary>
    public string Key { get; set; } = DbConfig.DefaultDbKey;

    /// <summary>
    /// 数据库类型：Sqlite、PostgreSQL、SqlServer 等（不区分大小写）
    /// </summary>
    public string DataType { get; set; } = "Sqlite";

    /// <summary>
    /// 主库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=configcenter.db";

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

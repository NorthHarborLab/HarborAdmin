namespace HarborAdmin.BuildingBlocks.EventBus.Configs;

/// <summary>
/// Harbor CAP 配置。
/// </summary>
public sealed class HarborCapOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Harbor:Cap";

    /// <summary>
    /// 传输方式：RabbitMq 或 InMemory。
    /// </summary>
    public string Transport { get; set; } = "RabbitMq";

    /// <summary>
    /// 默认消费者组。
    /// </summary>
    public string DefaultGroup { get; set; } = "harbor.default";

    /// <summary>
    /// CAP 版本号。
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// 失败重试次数。
    /// </summary>
    public int FailedRetryCount { get; set; } = 5;

    /// <summary>
    /// 失败重试间隔秒数。
    /// </summary>
    public int FailedRetryInterval { get; set; } = 15;

    /// <summary>
    /// 是否启用 CAP Dashboard。
    /// </summary>
    public bool UseDashboard { get; set; } = true;

    /// <summary>
    /// 消息存储配置。
    /// </summary>
    public CapStorageOptions Storage { get; set; } = new();

    /// <summary>
    /// RabbitMQ 连接配置。
    /// </summary>
    public RabbitMqOptions RabbitMq { get; set; } = new();

    /// <summary>
    /// Request/Reply 配置。
    /// </summary>
    public CapRequestReplyOptions RequestReply { get; set; } = new();
}

/// <summary>
/// CAP 存储配置。
/// </summary>
public sealed class CapStorageOptions
{
    /// <summary>
    /// 存储类型：Sqlite、InMemory 或 PostgreSql。
    /// </summary>
    public string Type { get; set; } = "Sqlite";

    /// <summary>
    /// 存储连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=../data/cap.db";
}

/// <summary>
/// RabbitMQ 连接配置。
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ 主机。
    /// </summary>
    public string HostName { get; set; } = "127.0.0.1";

    /// <summary>
    /// RabbitMQ 端口。
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ 用户名。
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ 密码。
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ 交换机名称。
    /// </summary>
    public string ExchangeName { get; set; } = "harbor.cap.default";
}

/// <summary>
/// CAP Request/Reply 配置。
/// </summary>
public sealed class CapRequestReplyOptions
{
    /// <summary>
    /// 是否启用 Request/Reply。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 当前服务名。
    /// </summary>
    public string ServiceName { get; set; } = AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// 当前服务实例 ID。
    /// </summary>
    public string InstanceId { get; set; } = Environment.MachineName;

    /// <summary>
    /// 默认超时秒数。
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 响应通道类型：InMemory、Redis、PostgreSql 或 MySql。
    /// </summary>
    public string Transport { get; set; } = "InMemory";

    /// <summary>
    /// 请求状态存储类型：InMemory、PostgreSql 或 MySql。
    /// </summary>
    public string Store { get; set; } = "InMemory";

    /// <summary>
    /// 是否启用 OpenTelemetry 诊断。
    /// </summary>
    public bool EnableOpenTelemetryDiagnostics { get; set; }

    /// <summary>
    /// Redis 响应通道配置。
    /// </summary>
    public CapRequestReplyRedisOptions Redis { get; set; } = new();

    /// <summary>
    /// PostgreSQL 响应通道与状态存储配置。
    /// </summary>
    public CapRequestReplyPostgreSqlOptions PostgreSql { get; set; } = new();

    /// <summary>
    /// MySQL 响应通道与状态存储配置。
    /// </summary>
    public CapRequestReplyMySqlOptions MySql { get; set; } = new();
}

/// <summary>
/// Request/Reply Redis 响应通道配置。
/// </summary>
public sealed class CapRequestReplyRedisOptions
{
    /// <summary>
    /// 当前服务作为请求方时写入 ReplyTo 的 Redis 逻辑端点名称。
    /// </summary>
    public string EndpointName { get; set; } = "default";

    /// <summary>
    /// Redis 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Reply Stream 名称前缀。
    /// </summary>
    public string StreamPrefix { get; set; } = "cap:reply";

    /// <summary>
    /// 额外 Redis 逻辑端点映射。
    /// </summary>
    public Dictionary<string, string> Endpoints { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Request/Reply PostgreSQL 配置。
/// </summary>
public sealed class CapRequestReplyPostgreSqlOptions
{
    /// <summary>
    /// PostgreSQL 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// PostgreSQL schema 名称。
    /// </summary>
    public string Schema { get; set; } = "cap";

    /// <summary>
    /// Reply 表名。
    /// </summary>
    public string ReplyTableName { get; set; } = "request_reply_inbox";

    /// <summary>
    /// PendingRequest 表名。
    /// </summary>
    public string StoreTableName { get; set; } = "request_reply";

    /// <summary>
    /// 是否在首次使用时自动创建表。
    /// </summary>
    public bool AutoCreateTable { get; set; } = true;
}

/// <summary>
/// Request/Reply MySQL 配置。
/// </summary>
public sealed class CapRequestReplyMySqlOptions
{
    /// <summary>
    /// MySQL 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// MySQL 表名前缀。
    /// </summary>
    public string TableNamePrefix { get; set; } = "cap";

    /// <summary>
    /// Reply 表名。
    /// </summary>
    public string ReplyTableName { get; set; } = "request_reply_inbox";

    /// <summary>
    /// PendingRequest 表名。
    /// </summary>
    public string StoreTableName { get; set; } = "request_reply";

    /// <summary>
    /// 是否在首次使用时自动创建表。
    /// </summary>
    public bool AutoCreateTable { get; set; } = true;
}

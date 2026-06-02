namespace HarborAdmin.BuildingBlocks.EventBus.Configs;

/// <summary>Harbor CAP 配置（<c>Cap</c> 节）。</summary>
public sealed class HarborCapOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Cap";

    /// <summary>传输：RabbitMq 或 InMemory。</summary>
    public string Transport { get; set; } = "RabbitMq";

    /// <summary>默认消费者组。</summary>
    public string DefaultGroup { get; set; } = "harbor.default";

    /// <summary>版本号（多开发者隔离）。</summary>
    public string Version { get; set; } = "v1";

    /// <summary>失败重试次数。</summary>
    public int FailedRetryCount { get; set; } = 5;

    /// <summary>失败重试间隔（秒）。</summary>
    public int FailedRetryInterval { get; set; } = 15;

    /// <summary>是否启用 Dashboard。</summary>
    public bool UseDashboard { get; set; } = true;

    /// <summary>消息存储。</summary>
    public CapStorageOptions Storage { get; set; } = new();

    /// <summary>RabbitMQ 连接。</summary>
    public RabbitMqOptions RabbitMq { get; set; } = new();
}

/// <summary>CAP 存储配置。</summary>
public sealed class CapStorageOptions
{
    /// <summary>Sqlite、InMemory、PostgreSql（预留）。</summary>
    public string Type { get; set; } = "Sqlite";

    /// <summary>连接字符串（Sqlite 等）。</summary>
    public string ConnectionString { get; set; } = "Data Source=../data/cap.db";
}

/// <summary>RabbitMQ 连接配置。</summary>
public sealed class RabbitMqOptions
{
    /// <summary>主机。</summary>
    public string HostName { get; set; } = "127.0.0.1";

    /// <summary>端口。</summary>
    public int Port { get; set; } = 5672;

    /// <summary>用户名。</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>密码。</summary>
    public string Password { get; set; } = "guest";

    /// <summary>交换机名称。</summary>
    public string ExchangeName { get; set; } = "harbor.cap.default";
}

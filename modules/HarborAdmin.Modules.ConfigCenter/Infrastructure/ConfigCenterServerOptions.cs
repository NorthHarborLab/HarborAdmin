namespace HarborAdmin.Modules.ConfigCenter.Infrastructure;

/// <summary>
/// ConfigCenter 服务运行选项(TCP 监听与数据库)
/// </summary>
public sealed class ConfigCenterServerOptions
{
    /// <summary>
    /// 配置节名称:<c>ConfigCenter</c>
    /// </summary>
    public const string SectionName = "ConfigCenter";

    /// <summary>
    /// TCP 监听地址,<c>0.0.0.0</c> 表示所有网卡
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// TCP 监听端口,默认 50000。
    /// </summary>
    public int Port { get; set; } = 50000;

    /// <summary>
    /// 数据库配置
    /// </summary>
    public ConfigCenterDatabaseOptions Database { get; set; } = new();
}

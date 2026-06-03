namespace HarborAdmin.ConfigCenter.Client;

/// <summary>
/// Harbor ConfigCenter 客户端连接选项(对应 <c>Harbor:ConfigCenter</c> 配置节)
/// </summary>
public sealed class ConfigCenterOptions
{
    /// <summary>
    /// 默认配置节路径
    /// </summary>
    public const string DefaultSectionName = "Harbor:ConfigCenter";

    /// <summary>
    /// ConfigCenter 服务主机名或 IP
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// ConfigCenter TCP 端口。
    /// </summary>
    public int Port { get; set; } = 50000;

    /// <summary>
    /// 本应用在配置中心注册的应用标识
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 环境名称，例如 <c>Development</c>
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// 可选客户端 ID；未设置时自动生成
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 断线后重连间隔（秒）
    /// </summary>
    public int ReconnectDelaySeconds { get; set; } = 5;
}

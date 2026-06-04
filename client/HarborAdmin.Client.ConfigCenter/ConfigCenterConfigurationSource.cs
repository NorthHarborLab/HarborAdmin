using Microsoft.Extensions.Configuration;

namespace HarborAdmin.Client.ConfigCenter;

/// <summary>
/// 将 <see cref="ConfigCenterConfigurationProvider"/> 注册到 <see cref="IConfigurationBuilder"/> 的配置源
/// </summary>
/// <param name="options">客户端连接选项(与配置节绑定)</param>
public sealed class ConfigCenterConfigurationSource(ConfigCenterOptions options) : IConfigurationSource
{
    /// <summary>
    /// 关联的配置提供程序实例
    /// </summary>
    public ConfigCenterConfigurationProvider Provider { get; } = new();

    /// <summary>
    /// 实现 <see cref="IConfigurationSource.Build"/>，返回本源的 <see cref="ConfigCenterConfigurationProvider"/>
    /// </summary>
    /// <param name="builder">配置构建器(未使用，保留接口签名)</param>
    /// <returns>远程配置提供程序实例</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;

    /// <summary>
    /// 选项快照
    /// </summary>
    public ConfigCenterOptions Options => options;
}

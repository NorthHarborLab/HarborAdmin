using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.ConfigCenter.Client;

/// <summary>
/// ConfigCenter 客户端 DI 与 <see cref="IConfigurationBuilder"/> 扩展
/// </summary>
public static class ConfigCenterServiceCollectionExtensions
{
    /// <summary>
    /// 向配置管道添加 ConfigCenter 远程配置源
    /// </summary>
    /// <param name="builder">配置构建器</param>
    /// <param name="section"><c>Harbor:ConfigCenter</c> 配置节</param>
    /// <returns>已注册的配置源，供 <see cref="AddHarborConfigCenter(IServiceCollection, ConfigCenterConfigurationSource, IConfigurationSection)"/> 使用</returns>
    public static ConfigCenterConfigurationSource AddHarborConfigCenter(this IConfigurationBuilder builder, IConfigurationSection section)
    {
        var options = section.Get<ConfigCenterOptions>() ?? new ConfigCenterOptions();
        var source = new ConfigCenterConfigurationSource(options);
        builder.Add(source);
        return source;
    }

    /// <summary>
    /// 注册 ConfigCenter 后台连接服务与选项绑定
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="source"><see cref="AddHarborConfigCenter"/> 返回的配置源</param>
    /// <param name="section">与配置源相同的配置节</param>
    /// <returns>原服务集合</returns>
    public static IServiceCollection AddHarborConfigCenter(this IServiceCollection services, ConfigCenterConfigurationSource source, IConfigurationSection section)
    {
        services.AddOptions<ConfigCenterOptions>().Configure(options => section.Bind(options));
        services.AddSingleton(source);
        services.AddHostedService<ConfigCenterConnectionHostedService>();
        return services;
    }
}
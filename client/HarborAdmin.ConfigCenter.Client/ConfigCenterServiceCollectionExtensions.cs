using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HarborAdmin.ConfigCenter.Client.Protocol;

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
    /// <returns>已注册的配置源，可供服务注册阶段继续使用</returns>
    public static ConfigCenterConfigurationSource AddHarborConfigCenter(this IConfigurationBuilder builder, IConfigurationSection section)
    {
        var options = section.Get<ConfigCenterOptions>() ?? new ConfigCenterOptions();
        var source = new ConfigCenterConfigurationSource(options);
        builder.Add(source);
        return source;
    }

    /// <summary>
    /// 启动期同步拉取一次 ConfigCenter 远程配置，并加入配置管道。
    /// </summary>
    /// <param name="builder">配置构建器</param>
    /// <param name="section"><c>Harbor:ConfigCenter</c> 配置节</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已注册的配置源</returns>
    public static async Task<ConfigCenterConfigurationSource> AddHarborConfigCenterAsync(
        this IConfigurationBuilder builder,
        IConfigurationSection section,
        CancellationToken cancellationToken = default)
    {
        var options = section.Get<ConfigCenterOptions>() ?? new ConfigCenterOptions();
        var source = new ConfigCenterConfigurationSource(options);
        builder.Add(source);

        var data = await LoadRemoteConfigurationAsync(options, cancellationToken);
        source.Provider.SetData(data);
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

    private static async Task<IReadOnlyDictionary<string, string>> LoadRemoteConfigurationAsync(
        ConfigCenterOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.AppId))
        {
            throw new InvalidOperationException("Harbor ConfigCenter AppId is required.");
        }

        await using var client = new ConfigTcpClient();
        await client.ConnectAsync(options.Host, options.Port, cancellationToken);

        var clientId = options.ClientId ?? $"{options.AppId}-{Environment.MachineName}-{Guid.NewGuid():N}";
        await client.SendAsync(
            ConfigMessage.HandshakeRequest(options.AppId, options.Environment, clientId),
            cancellationToken);

        _ = await ReadExpectedAsync(client, ConfigMessageTypes.Handshake, cancellationToken);

        await client.SendAsync(ConfigMessage.GetConfigRequest(), cancellationToken);
        var response = await ReadExpectedAsync(client, ConfigMessageTypes.GetConfigResponse, cancellationToken);
        return response.Data ?? new Dictionary<string, string>();
    }

    private static async Task<ConfigMessage> ReadExpectedAsync(
        ConfigTcpClient client,
        string expectedType,
        CancellationToken cancellationToken)
    {
        var message = await client.ReceiveAsync(cancellationToken)
                      ?? throw new InvalidOperationException("Connection closed while waiting for ConfigCenter response.");

        if (message.Type == ConfigMessageTypes.Error)
        {
            throw new InvalidOperationException(message.Message ?? "ConfigCenter returned an error.");
        }

        if (message.Type != expectedType)
        {
            throw new InvalidOperationException($"Expected ConfigCenter message '{expectedType}' but received '{message.Type}'.");
        }

        return message;
    }
}

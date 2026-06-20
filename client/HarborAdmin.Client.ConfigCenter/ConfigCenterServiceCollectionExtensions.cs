using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HarborAdmin.Client.ConfigCenter.Protocol;

namespace HarborAdmin.Client.ConfigCenter;

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

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.InitialLoadTimeoutSeconds)));
            var response = await LoadRemoteConfigurationAsync(options, timeoutCts.Token);
            source.Provider.SetData(response.Data ?? new Dictionary<string, string>(), response.Version);
        }
        catch when (!options.Required)
        {
            // Host 等管理进程可选择不因 ConfigCenter 暂不可用而阻断启动,后台服务会继续重连。
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Failed to load required Harbor ConfigCenter configuration for AppId '{options.AppId}' from {options.Host}:{options.Port}.",
                ex);
        }

        return source;
    }

    /// <summary>
    /// 注册 ConfigCenter 后台连接服务与选项绑定
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="source"><see cref="AddHarborConfigCenter(IConfigurationBuilder,IConfigurationSection)"/> 返回的配置源</param>
    /// <param name="section">与配置源相同的配置节</param>
    /// <returns>原服务集合</returns>
    public static IServiceCollection AddHarborConfigCenter(this IServiceCollection services, ConfigCenterConfigurationSource source, IConfigurationSection section)
    {
        services.AddOptions<ConfigCenterOptions>().Configure(options => section.Bind(options));
        services.AddSingleton(source);
        services.AddSingleton<ConfigCenterClientState>();
        services.AddSingleton<IConfigCenterClientState>(sp => sp.GetRequiredService<ConfigCenterClientState>());
        services.AddHostedService<ConfigCenterConnectionHostedService>();
        return services;
    }

    /// <summary>
    /// 建立一次性 TCP 连接并读取启动期远程配置快照。
    /// </summary>
    /// <param name="options">配置中心客户端选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>远程配置响应</returns>
    private static async Task<ConfigMessage> LoadRemoteConfigurationAsync(
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
            ConfigMessage.HelloRequest(options.AppId, clientId),
            cancellationToken);

        _ = await ReadExpectedAsync(client, ConfigMessageTypes.Hello, cancellationToken);

        await client.SendAsync(ConfigMessage.GetConfigRequest(), cancellationToken);
        return await ReadExpectedAsync(client, ConfigMessageTypes.GetConfigResult, cancellationToken);
    }

    /// <summary>
    /// 读取并校验配置中心返回的指定类型消息。
    /// </summary>
    /// <param name="client">配置中心 TCP 客户端</param>
    /// <param name="expectedType">期望的消息类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配期望类型的消息</returns>
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

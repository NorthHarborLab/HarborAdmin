using System.Net;
using System.Net.Sockets;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// ConfigCenter TCP 监听后台服务：接受连接并为每条连接创建 <see cref="ConfigCenterConnectionHandler"/>。
/// </summary>
/// <param name="options">监听地址与端口配置</param>
/// <param name="serviceProvider">用于为每条连接创建作用域并解析依赖</param>
/// <param name="logger">日志</param>
public sealed class ConfigCenterTcpHostedService(
    IOptions<ConfigCenterServerOptions> options,
    IServiceProvider serviceProvider,
    ILogger<ConfigCenterTcpHostedService> logger) : BackgroundService
{
    /// <summary>
    /// TCP 监听器
    /// </summary>
    private TcpListener? _listener;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var listenAddress = settings.Host is "0.0.0.0" or "*"
            ? IPAddress.Any
            : IPAddress.Parse(settings.Host);
        _listener = new TcpListener(listenAddress, settings.Port);
        _listener.Start();
        logger.LogInformation("ConfigCenter TCP listening on {Host}:{Port}", settings.Host, settings.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(async () =>
                {
                    // 每条 TCP 连接使用独立 DI scope，避免 Scoped 服务在多个连接之间共享状态。
                    await using var scope = serviceProvider.CreateAsyncScope();
                    var handler = new ConfigCenterConnectionHandler(
                        client,
                        scope.ServiceProvider.GetRequiredService<PublishedConfigCache>(),
                        scope.ServiceProvider.GetRequiredService<IConfigCenterRepository>(),
                        scope.ServiceProvider.GetRequiredService<ConfigSubscriptionHub>(),
                        scope.ServiceProvider.GetRequiredService<ILogger<ConfigCenterConnectionHandler>>());

                    await handler.RunAsync(stoppingToken);
                }, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 正常停止
        }
        finally
        {
            _listener.Stop();
        }
    }
}
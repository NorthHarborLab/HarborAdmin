using HarborAdmin.Client.ConfigCenter.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Client.ConfigCenter;

/// <summary>
/// 后台维护与 ConfigCenter 的 TCP 长连接:hello、拉取配置、订阅变更、心跳与自动重连。
/// </summary>
/// <param name="options">客户端选项</param>
/// <param name="source">配置源(用于更新 Provider)</param>
/// <param name="state">客户端运行状态</param>
/// <param name="logger">日志</param>
public sealed class ConfigCenterConnectionHostedService(
    IOptions<ConfigCenterOptions> options,
    ConfigCenterConfigurationSource source,
    ConfigCenterClientState state,
    ILogger<ConfigCenterConnectionHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AppId))
        {
            const string message = "Harbor ConfigCenter AppId is not configured; remote configuration is disabled.";
            state.MarkDisconnected(message);
            logger.LogWarning(message);
            return;
        }

        var clientId = settings.ClientId ?? $"{settings.AppId}-{Environment.MachineName}-{Guid.NewGuid():N}";
        state.Configure(settings.AppId, clientId);

        var initialDelay = TimeSpan.FromSeconds(Math.Max(1, settings.ReconnectInitialDelaySeconds));
        var maxDelay = TimeSpan.FromSeconds(Math.Max(initialDelay.TotalSeconds, settings.ReconnectMaxDelaySeconds));
        var heartbeatInterval = TimeSpan.FromSeconds(Math.Max(5, settings.HeartbeatSeconds));
        var delay = initialDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var client = new ConfigTcpClient();
            using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            Task? heartbeatTask = null;
            string? disconnectError = null;

            try
            {
                await client.ConnectAsync(settings.Host, settings.Port, stoppingToken);
                await client.SendAsync(ConfigMessage.HelloRequest(settings.AppId, clientId), stoppingToken);
                _ = await ReadExpectedAsync(client, ConfigMessageTypes.Hello, stoppingToken);

                state.MarkConnected();
                delay = initialDelay;
                heartbeatTask = RunHeartbeatAsync(client, heartbeatInterval, connectionCts);

                await RefreshConfigurationAsync(client, stoppingToken);
                await client.SendAsync(ConfigMessage.SubscribeRequest(), stoppingToken);

                while (!stoppingToken.IsCancellationRequested && !connectionCts.IsCancellationRequested)
                {
                    var message = await client.ReceiveAsync(connectionCts.Token);
                    if (message is null)
                    {
                        disconnectError = "ConfigCenter connection closed.";
                        break;
                    }

                    switch (message.Type)
                    {
                        case ConfigMessageTypes.ConfigChanged:
                            if (message.Version > 0 && message.Version == source.Provider.Version)
                            {
                                logger.LogDebug("Ignored ConfigCenter version {Version} because it is already applied.", message.Version);
                                break;
                            }

                            await RefreshConfigurationAsync(client, stoppingToken);
                            break;
                        case ConfigMessageTypes.Pong:
                        case ConfigMessageTypes.GetConfigResult:
                            break;
                        case ConfigMessageTypes.Error:
                            logger.LogWarning("ConfigCenter error: {Message}", message.Message);
                            break;
                        default:
                            logger.LogDebug("Ignored ConfigCenter message type {Type}", message.Type);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException) when (connectionCts.IsCancellationRequested)
            {
                disconnectError = "ConfigCenter heartbeat failed.";
            }
            catch (Exception ex)
            {
                disconnectError = ex.Message;
                logger.LogWarning(ex, "ConfigCenter connection failed; retrying in {Delay}", delay);
            }
            finally
            {
                await connectionCts.CancelAsync();
                if (heartbeatTask is not null)
                {
                    try
                    {
                        await heartbeatTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // 连接关闭时退出心跳任务。
                    }
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    state.MarkDisconnected(disconnectError);
                }
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(delay, stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Min(maxDelay.TotalSeconds, delay.TotalSeconds * 2));
            }
        }
    }

    private async Task RefreshConfigurationAsync(ConfigTcpClient client, CancellationToken cancellationToken)
    {
        await client.SendAsync(ConfigMessage.GetConfigRequest(), cancellationToken);
        var response = await ReadExpectedAsync(client, ConfigMessageTypes.GetConfigResult, cancellationToken);
        var data = response.Data ?? new Dictionary<string, string>();
        var applied = source.Provider.SetData(data, response.Version);
        state.MarkReloaded(source.Provider.Version, source.Provider.GetAllData());

        if (applied)
        {
            logger.LogInformation("ConfigCenter configuration reloaded, version {Version}.", response.Version);
        }
    }

    private async Task RunHeartbeatAsync(
        ConfigTcpClient client,
        TimeSpan interval,
        CancellationTokenSource connectionCts)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(connectionCts.Token))
        {
            try
            {
                await client.SendAsync(ConfigMessage.PingRequest(), connectionCts.Token);
            }
            catch when (!connectionCts.IsCancellationRequested)
            {
                await connectionCts.CancelAsync();
                throw;
            }
        }
    }

    private static async Task<ConfigMessage> ReadExpectedAsync(ConfigTcpClient client, string expectedType, CancellationToken cancellationToken)
    {
        while (true)
        {
            var message = await client.ReceiveAsync(cancellationToken)
                          ?? throw new InvalidOperationException("Connection closed while waiting for response.");

            if (message.Type == ConfigMessageTypes.Pong)
            {
                continue;
            }

            if (message.Type == ConfigMessageTypes.Error)
            {
                throw new InvalidOperationException(message.Message ?? "ConfigCenter returned an error.");
            }

            if (message.Type != expectedType)
            {
                throw new InvalidOperationException($"Expected '{expectedType}' but received '{message.Type}'.");
            }

            return message;
        }
    }
}

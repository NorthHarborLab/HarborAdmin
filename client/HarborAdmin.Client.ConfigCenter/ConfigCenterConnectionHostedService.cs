using HarborAdmin.Client.ConfigCenter.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Client.ConfigCenter;

/// <summary>
/// 后台维护与 ConfigCenter 的 TCP 长连接:握手、拉取配置、订阅变更并在变更时刷新 <see cref="ConfigCenterConfigurationProvider"/>
/// </summary>
/// <param name="options">客户端选项</param>
/// <param name="source">配置源(用于更新 Provider)</param>
/// <param name="logger">日志</param>
public sealed class ConfigCenterConnectionHostedService(
    IOptions<ConfigCenterOptions> options,
    ConfigCenterConfigurationSource source,
    ILogger<ConfigCenterConnectionHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AppId))
        {
            logger.LogWarning("Harbor ConfigCenter AppId is not configured; remote configuration is disabled.");
            return;
        }

        var clientId = settings.ClientId ?? $"{settings.AppId}-{Environment.MachineName}-{Guid.NewGuid():N}";
        var delay = TimeSpan.FromSeconds(Math.Max(1, settings.ReconnectDelaySeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var client = new ConfigTcpClient();
            try
            {
                await client.ConnectAsync(settings.Host, settings.Port, stoppingToken);
                await client.SendAsync(
                    ConfigMessage.HandshakeRequest(settings.AppId, settings.Environment, clientId),
                    stoppingToken);

                var handshakeResponse = await client.ReceiveAsync(stoppingToken);
                if (handshakeResponse?.Type == ConfigMessageTypes.Error)
                {
                    throw new InvalidOperationException(handshakeResponse.Message ?? "Handshake failed.");
                }

                await client.SendAsync(ConfigMessage.GetConfigRequest(), stoppingToken);
                var configResponse = await ReadExpectedAsync(client, ConfigMessageTypes.GetConfigResponse, stoppingToken);
                if (configResponse.Data is not null)
                {
                    source.Provider.SetData(configResponse.Data);
                }

                await client.SendAsync(ConfigMessage.SubscribeRequest(), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var message = await client.ReceiveAsync(stoppingToken);
                    if (message is null)
                    {
                        break;
                    }

                    switch (message.Type)
                    {
                        case ConfigMessageTypes.ConfigChanged:
                            await client.SendAsync(ConfigMessage.GetConfigRequest(), stoppingToken);
                            var refreshed = await ReadExpectedAsync(client, ConfigMessageTypes.GetConfigResponse, stoppingToken);
                            if (refreshed.Data is not null)
                            {
                                source.Provider.SetData(refreshed.Data);
                            }

                            break;
                        case ConfigMessageTypes.Pong:
                        case ConfigMessageTypes.GetConfigResponse:
                            break;
                        case ConfigMessageTypes.Error:
                            logger.LogWarning("ConfigCenter error: {Message}", message.Message);
                            break;
                        default:
                            logger.LogDebug("Ignored message type {Type}", message.Type);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ConfigCenter connection failed; retrying in {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    /// <summary>
    /// 读取指定类型的下一条消息，遇 error 或类型不符则抛异常
    /// </summary>
    private static async Task<ConfigMessage> ReadExpectedAsync(ConfigTcpClient client, string expectedType, CancellationToken cancellationToken)
    {
        var message = await client.ReceiveAsync(cancellationToken)
                      ?? throw new InvalidOperationException("Connection closed while waiting for response.");

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
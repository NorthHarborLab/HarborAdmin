using HarborAdmin.ConfigCenter.Client.Protocol;
using HarborAdmin.Modules.ConfigCenter.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure;

/// <summary>
/// 通过 TCP 短连接向 ConfigCenter 进程发送 <c>publishNotify</c> 的实现(供 Host 使用)   
/// </summary>
/// <param name="options">服务地址配置</param>
/// <param name="logger">日志</param>
public sealed class TcpConfigCenterNotifyClient(
    IOptions<ConfigCenterServerOptions> options,
    ILogger<TcpConfigCenterNotifyClient> logger) : IConfigCenterNotifyClient
{
    /// <inheritdoc />
    public async Task NotifyPublishedAsync(
        string appId,
        string environment,
        long releaseId,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        await using var client = new ConfigTcpClient();
        try
        {
            await client.ConnectAsync(settings.Host == "0.0.0.0" ? "127.0.0.1" : settings.Host, settings.Port, cancellationToken);
            await client.SendAsync(ConfigMessage.PublishNotifyRequest(appId, environment, releaseId), cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await client.ReceiveAsync(timeoutCts.Token);
            if (response?.Type != ConfigMessageTypes.PublishNotifyAck || !response.Ok)
            {
                var message = response?.Message ?? "No response from ConfigCenter.";
                throw new InvalidOperationException($"ConfigCenter publish notify failed: {message}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to notify ConfigCenter for {AppId}/{Environment} release {ReleaseId}", appId, environment, releaseId);
            throw;
        }
    }
}

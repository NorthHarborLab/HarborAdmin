using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Client.ConfigCenter.Protocol;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients;

/// <summary>
/// 通过 TCP 短连接向 ConfigCenter 进程发送 <c>publishNotify</c> 的实现(供 Host 使用)   
/// </summary>
/// <param name="options">服务地址配置</param>
/// <param name="logger">日志</param>
public sealed class TcpConfigCenterNotifyClient(IOptions<ConfigCenterServerOptions> options, ILogger<TcpConfigCenterNotifyClient> logger) : IConfigCenterNotifyClient
{
    /// <inheritdoc />
    public async Task NotifyPublishedAsync(string appId, long releaseId, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        await using var client = new ConfigTcpClient();
        try
        {
            await client.ConnectAsync(settings.Host == "0.0.0.0" ? "127.0.0.1" : settings.Host, settings.Port, cancellationToken);
            await client.SendAsync(ConfigMessage.PublishNotifyRequest(appId, releaseId), cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await client.ReceiveAsync(timeoutCts.Token);
            if (response?.Type != ConfigMessageTypes.PublishNotifyAck || !response.Ok)
            {
                var message = response?.Message ?? "No response from ConfigCenter.";
                throw new BusinessDomainException(
                    ApiResultCodes.InternalError,
                    $"ConfigCenter publish notify failed: {message}",
                    StatusCodes.Status502BadGateway);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to notify ConfigCenter for {AppId} release {ReleaseId}", appId, releaseId);
            throw;
        }
    }
}
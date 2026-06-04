using System.Net.Sockets;
using HarborAdmin.Client.ConfigCenter.Protocol;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 处理单条 TCP 连接上的 ConfigCenter JSON 帧协议。
/// </summary>
/// <param name="tcpClient">接受的 TCP 客户端</param>
/// <param name="cache">已发布配置缓存</param>
/// <param name="repository">仓储(hello 时校验应用是否存在)</param>
/// <param name="subscriptionHub">订阅广播中心</param>
/// <param name="logger">日志</param>
public sealed class ConfigCenterConnectionHandler(
    TcpClient tcpClient,
    PublishedConfigCache cache,
    IConfigCenterRepository repository,
    ConfigSubscriptionHub subscriptionHub,
    ILogger<ConfigCenterConnectionHandler> logger)
{
    private readonly ConfigFrameReader _frameReader = new();
    private NetworkStream? _stream;
    private string? _appId;
    private Guid? _subscriptionId;

    /// <summary>
    /// 处理连接直到关闭或取消。
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _stream = tcpClient.GetStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _frameReader.ReadFrameAsync(_stream, cancellationToken);
                if (message is null)
                {
                    break;
                }

                await HandleMessageAsync(message, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常关闭。
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ConfigCenter connection error");
        }
        finally
        {
            if (_subscriptionId is { } subscriptionId)
            {
                subscriptionHub.Unregister(subscriptionId);
            }

            tcpClient.Close();
            tcpClient.Dispose();
        }
    }

    private async Task HandleMessageAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (message.ProtocolVersion != ConfigMessage.CurrentProtocolVersion)
        {
            await SendErrorAsync(
                message.RequestId,
                "unsupported_protocol",
                $"Protocol version {message.ProtocolVersion} is not supported.",
                cancellationToken);
            return;
        }

        switch (message.Type)
        {
            case ConfigMessageTypes.Hello:
                await HandleHelloAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.GetConfig:
                await HandleGetConfigAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.Subscribe:
                await HandleSubscribeAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.PublishNotify:
                await HandlePublishNotifyAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.Ping:
                await SendAsync(new ConfigMessage
                {
                    Type = ConfigMessageTypes.Pong,
                    RequestId = message.RequestId,
                    Ok = true
                }, cancellationToken);
                break;
            default:
                await SendErrorAsync(message.RequestId, "unsupported_message", $"Unsupported message type '{message.Type}'.",
                    cancellationToken);
                break;
        }
    }

    private async Task HandleHelloAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.AppId))
        {
            await SendErrorAsync(message.RequestId, "invalid_hello", "appId is required.", cancellationToken);
            return;
        }

        var app = await repository.GetApplicationByAppIdAsync(message.AppId.Trim(), cancellationToken);
        if (app is null)
        {
            await SendErrorAsync(message.RequestId, "unknown_app", $"Application '{message.AppId}' not found.", cancellationToken);
            return;
        }

        _appId = message.AppId.Trim();
        await SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.Hello,
            RequestId = message.RequestId,
            Ok = true,
            AppId = _appId,
            ClientId = message.ClientId
        }, cancellationToken);
    }

    private async Task HandleGetConfigAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (_appId is null)
        {
            await SendErrorAsync(message.RequestId, "not_hello", "hello is required before getConfig.", cancellationToken);
            return;
        }

        var snapshot = await cache.GetOrLoadAsync(_appId, message.Version, cancellationToken);
        await SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.GetConfigResult,
            RequestId = message.RequestId,
            Ok = true,
            AppId = _appId,
            Version = snapshot?.Version ?? 0,
            Data = snapshot?.Data.ToDictionary(static x => x.Key, static x => x.Value)
                   ?? new Dictionary<string, string>()
        }, cancellationToken);
    }

    private async Task HandleSubscribeAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (_appId is null || _stream is null)
        {
            await SendErrorAsync(message.RequestId, "not_hello", "hello is required before subscribe.", cancellationToken);
            return;
        }

        if (_subscriptionId is { } existing)
        {
            subscriptionHub.Unregister(existing);
        }

        _subscriptionId = subscriptionHub.Register(_appId, _stream);
    }

    private async Task HandlePublishNotifyAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.AppId))
        {
            await SendErrorAsync(message.RequestId, "invalid_notify", "appId is required.", cancellationToken);
            return;
        }

        var appId = message.AppId.Trim();
        await cache.RefreshAsync(appId, message.ReleaseId > 0 ? message.ReleaseId : null, cancellationToken);
        var snapshot = await cache.GetOrLoadAsync(appId, cancellationToken: cancellationToken);
        var version = snapshot?.Version ?? 0;

        await SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.PublishNotifyAck,
            RequestId = message.RequestId,
            Ok = true,
            AppId = appId,
            ReleaseId = message.ReleaseId,
            Version = version
        }, cancellationToken);

        await subscriptionHub.BroadcastConfigChangedAsync(appId, version, cancellationToken);
    }

    private Task SendErrorAsync(string? requestId, string code, string errorMessage, CancellationToken cancellationToken) =>
        SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.Error,
            RequestId = requestId,
            Code = code,
            Message = errorMessage
        }, cancellationToken);

    private async Task SendAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        var frame = message.ToFrameBytes();
        await _stream.WriteAsync(frame, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }
}

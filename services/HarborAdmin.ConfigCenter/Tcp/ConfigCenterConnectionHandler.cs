using System.Net.Sockets;
using HarborAdmin.ConfigCenter.Client.Protocol;
using HarborAdmin.Modules.ConfigCenter.Contracts;
using HarborAdmin.Modules.ConfigCenter.Infrastructure;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 处理单条 TCP 连接上的 ConfigCenter JSON 帧协议,用于处理握手,获取配置,订阅配置变更,发送配置变更通知等操作
/// </summary>
/// <param name="tcpClient">接受的 TCP 客户端</param>
/// <param name="cache">已发布配置缓存</param>
/// <param name="repository">仓储(握手时校验应用是否存在)</param>
/// <param name="subscriptionHub">订阅广播中心</param>
/// <param name="logger">日志</param>
public sealed class ConfigCenterConnectionHandler(
    TcpClient tcpClient,
    PublishedConfigCache cache,
    IConfigCenterRepository repository,
    ConfigSubscriptionHub subscriptionHub,
    ILogger<ConfigCenterConnectionHandler> logger)
{
    /// <summary>
    /// 帧读取器
    /// </summary>
    private readonly ConfigFrameReader _frameReader = new();

    /// <summary>
    /// 当前连接的网络流
    /// </summary>
    private NetworkStream? _stream;

    /// <summary>
    /// 握手后的应用标识
    /// </summary>
    private string? _appId;

    /// <summary>
    /// 握手后的环境名称
    /// </summary>
    private string? _environment;

    /// <summary>
    /// 当前连接在 <see cref="ConfigSubscriptionHub"/> 中的订阅 ID
    /// </summary>
    private Guid? _subscriptionId;

    /// <summary>
    /// 处理连接直到关闭或取消
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
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
            // 正常关闭
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

    /// <summary>
    /// 按消息类型分发处理
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandleMessageAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case ConfigMessageTypes.Handshake:
                await HandleHandshakeAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.GetConfig:
                await HandleGetConfigAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.Subscribe:
                HandleSubscribe();
                break;
            case ConfigMessageTypes.PublishNotify:
                await HandlePublishNotifyAsync(message, cancellationToken);
                break;
            case ConfigMessageTypes.Ping:
                await SendAsync(new ConfigMessage { Type = ConfigMessageTypes.Pong }, cancellationToken);
                break;
            default:
                await SendErrorAsync("unsupported_message", $"Unsupported message type '{message.Type}'.",
                    cancellationToken);
                break;
        }
    }

    /// <summary>
    /// 处理握手:校验应用存在并绑定当前连接的 appId/environment
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandleHandshakeAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.AppId) || string.IsNullOrWhiteSpace(message.Environment))
        {
            await SendErrorAsync("invalid_handshake", "appId and environment are required.", cancellationToken);
            return;
        }

        var app = await repository.GetApplicationByAppIdAsync(message.AppId.Trim(), cancellationToken);
        if (app is null)
        {
            await SendErrorAsync("unknown_app", $"Application '{message.AppId}' not found.", cancellationToken);
            return;
        }

        _appId = message.AppId.Trim();
        _environment = message.Environment.Trim();

        await SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.Handshake,
            Ok = true,
            AppId = _appId,
            Environment = _environment
        }, cancellationToken);
    }

    /// <summary>
    /// 返回已发布配置快照(需先握手)
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandleGetConfigAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (_appId is null || _environment is null)
        {
            await SendErrorAsync("not_handshaked", "Handshake is required before getConfig.", cancellationToken);
            return;
        }

        var snapshot = await cache.GetOrLoadAsync(_appId, _environment, message.Version, cancellationToken);
        if (snapshot is null)
        {
            await SendAsync(new ConfigMessage
            {
                Type = ConfigMessageTypes.GetConfigResponse,
                Version = 0,
                Data = new Dictionary<string, string>()
            }, cancellationToken);
            return;
        }

        await SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.GetConfigResponse,
            Version = snapshot.Version,
            Data = snapshot.Data.ToDictionary(static x => x.Key, static x => x.Value)
        }, cancellationToken);
    }

    /// <summary>
    /// 将当前长连接注册为配置变更订阅者
    /// </summary>
    private void HandleSubscribe()
    {
        if (_appId is null || _environment is null || _stream is null)
        {
            return;
        }

        if (_subscriptionId is { } existing)
        {
            subscriptionHub.Unregister(existing);
        }

        _subscriptionId = subscriptionHub.Register(_appId, _environment, _stream);
    }

    /// <summary>
    /// 处理 Host 发来的发布通知:刷新缓存/回复 ack/广播 <c>configChanged</c>。
    /// </summary>
    private async Task HandlePublishNotifyAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.AppId) || string.IsNullOrWhiteSpace(message.Environment))
        {
            await SendErrorAsync("invalid_notify", "appId and environment are required.", cancellationToken);
            return;
        }

        var appId = message.AppId.Trim();
        var environment = message.Environment.Trim();

        await cache.RefreshAsync(appId, environment, message.ReleaseId > 0 ? message.ReleaseId : null,
            cancellationToken);
        var snapshot = await cache.GetOrLoadAsync(appId, environment, cancellationToken: cancellationToken);
        var version = snapshot?.Version ?? 0;

        await SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.PublishNotifyAck,
            Ok = true,
            AppId = appId,
            Environment = environment,
            Version = version
        }, cancellationToken);

        await subscriptionHub.BroadcastConfigChangedAsync(appId, environment, version, cancellationToken);
    }

    /// <summary>
    /// 发送错误帧
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    private Task SendErrorAsync(string code, string errorMessage, CancellationToken cancellationToken) =>
        SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.Error,
            Code = code,
            Message = errorMessage
        }, cancellationToken);

    /// <summary>
    /// 将消息编码为帧并写入连接
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
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
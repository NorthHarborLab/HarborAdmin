using System.Net.Sockets;
using HarborAdmin.Client.ConfigCenter.Protocol;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

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
                    // 空帧表示对端已关闭或没有可处理消息，退出循环后统一清理订阅和 TCP 资源。
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

    /// <summary>
    /// 按协议版本与消息类型分派当前帧。
    /// </summary>
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

    /// <summary>
    /// 处理握手消息并校验应用是否已在配置中心登记。
    /// </summary>
    private async Task HandleHelloAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.AppId))
        {
            await SendErrorAsync(message.RequestId, "invalid_hello", "appId is required.", cancellationToken);
            return;
        }

        // hello 是连接级身份绑定点；未知 appId 不允许继续拉取或订阅。
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

    /// <summary>
    /// 处理配置拉取请求。
    /// </summary>
    private async Task HandleGetConfigAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (_appId is null)
        {
            await SendErrorAsync(message.RequestId, "not_hello", "hello is required before getConfig.", cancellationToken);
            return;
        }

        PublishedConfigSnapshot? snapshot;
        try
        {
            // version=0 走最新缓存；指定版本直接读数据库，避免历史版本污染最新快照缓存。
            snapshot = await cache.GetOrLoadAsync(_appId, message.Version, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Secret 解析失败属于运行时配置不可用，返回协议错误而不是断开整个进程。
            logger.LogWarning(ex, "ConfigCenter secret resolution failed for app {AppId}.", _appId);
            await SendErrorAsync(message.RequestId, "secret_resolution_failed", "Secret reference cannot be resolved.", cancellationToken);
            return;
        }

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

    /// <summary>
    /// 处理配置变更订阅请求。
    /// </summary>
    private async Task HandleSubscribeAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (_appId is null || _stream is null)
        {
            await SendErrorAsync(message.RequestId, "not_hello", "hello is required before subscribe.", cancellationToken);
            return;
        }

        if (_subscriptionId is { } existing)
        {
            // 同一连接重复订阅时只保留最新登记，避免一次变更向同一连接重复写入。
            subscriptionHub.Unregister(existing);
        }

        _subscriptionId = subscriptionHub.Register(_appId, _stream);
    }

    /// <summary>
    /// 处理 Host 发布完成通知，刷新缓存并广播订阅方。
    /// </summary>
    private async Task HandlePublishNotifyAsync(ConfigMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.AppId))
        {
            await SendErrorAsync(message.RequestId, "invalid_notify", "appId is required.", cancellationToken);
            return;
        }

        var appId = message.AppId.Trim();
        PublishedConfigSnapshot? snapshot;
        try
        {
            // Host 已经提交发布事务；优先按 releaseId 读取刚发布的快照，避免并发发布时误取旧版本。
            await cache.RefreshAsync(appId, message.ReleaseId > 0 ? message.ReleaseId : null, cancellationToken);
            snapshot = await cache.GetOrLoadAsync(appId, cancellationToken: cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "ConfigCenter secret resolution failed for publish notify {AppId}.", appId);
            await SendErrorAsync(message.RequestId, "secret_resolution_failed", "Secret reference cannot be resolved.", cancellationToken);
            return;
        }

        var version = snapshot?.Version ?? 0;

        // 先确认 Host 的 publishNotify，再广播客户端；Host 只需要知道缓存刷新已经完成。
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

    /// <summary>
    /// 写入标准协议错误响应。
    /// </summary>
    private Task SendErrorAsync(string? requestId, string code, string errorMessage, CancellationToken cancellationToken) =>
        SendAsync(new ConfigMessage
        {
            Type = ConfigMessageTypes.Error,
            RequestId = requestId,
            Code = code,
            Message = errorMessage
        }, cancellationToken);

    /// <summary>
    /// 将消息编码为协议帧并写入当前连接。
    /// </summary>
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

using System.Collections.Concurrent;
using System.Net.Sockets;
using HarborAdmin.Client.ConfigCenter.Protocol;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 管理已订阅配置变更的 TCP 连接,并负责广播 <c>configChanged</c> 消息。
/// </summary>
public sealed class ConfigSubscriptionHub
{
    private readonly ConcurrentDictionary<Guid, SubscriptionEntry> _subscriptions = new();

    /// <summary>
    /// 注册一条订阅。
    /// </summary>
    public Guid Register(string appId, NetworkStream stream)
    {
        var id = Guid.NewGuid();
        _subscriptions[id] = new SubscriptionEntry(appId, stream);
        return id;
    }

    /// <summary>
    /// 移除订阅。
    /// </summary>
    public void Unregister(Guid id) => _subscriptions.TryRemove(id, out _);

    /// <summary>
    /// 向匹配 <paramref name="appId"/> 的所有订阅连接广播配置变更。
    /// </summary>
    public async Task BroadcastConfigChangedAsync(string appId, int version, CancellationToken cancellationToken)
    {
        var message = new ConfigMessage
        {
            Type = ConfigMessageTypes.ConfigChanged,
            Ok = true,
            AppId = appId,
            Version = version
        };

        var frame = message.ToFrameBytes();
        // 先复制目标列表，避免广播过程中订阅集合变化影响本次遍历。
        var targets = _subscriptions.Values
            .Where(s => s.AppId == appId)
            .ToList();

        foreach (var target in targets)
        {
            try
            {
                await target.Stream.WriteAsync(frame, cancellationToken);
                await target.Stream.FlushAsync(cancellationToken);
            }
            catch
            {
                // 写入失败时由连接处理循环负责清理断开的连接。
            }
        }
    }

    /// <summary>
    /// 单条配置变更订阅。
    /// </summary>
    private sealed record SubscriptionEntry(string AppId, NetworkStream Stream);
}

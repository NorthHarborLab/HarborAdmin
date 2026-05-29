using System.Collections.Concurrent;
using System.Net.Sockets;
using HarborAdmin.ConfigCenter.Client.Protocol;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 管理已订阅配置变更的 TCP 连接,并负责广播 <c>configChanged</c> 消息
/// </summary>
public sealed class ConfigSubscriptionHub
{
    /// <summary>
    /// 订阅表:订阅 ID → 连接信息
    /// </summary>
    private readonly ConcurrentDictionary<Guid, SubscriptionEntry> _subscriptions = new();

    /// <summary>
    /// 注册一条订阅（客户端在 <c>subscribe</c> 后调用）
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="stream">该连接的网络流,用于推送消息</param>
    /// <returns>订阅 ID，连接断开时需 <see cref="Unregister"/></returns>
    public Guid Register(string appId, string environment, NetworkStream stream)
    {
        var id = Guid.NewGuid();
        _subscriptions[id] = new SubscriptionEntry(appId, environment, stream);
        return id;
    }

    /// <summary>
    /// 移除订阅
    /// </summary>
    /// <param name="id"><see cref="Register"/> 返回的订阅 ID</param>
    public void Unregister(Guid id) => _subscriptions.TryRemove(id, out _);

    /// <summary>
    /// 向匹配 <paramref name="appId"/> 与 <paramref name="environment"/> 的所有订阅连接广播配置变更
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="version">新发布版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BroadcastConfigChangedAsync(string appId, string environment, int version,
        CancellationToken cancellationToken)
    {
        var message = new ConfigMessage
        {
            Type = ConfigMessageTypes.ConfigChanged,
            AppId = appId,
            Environment = environment,
            Version = version
        };

        var frame = message.ToFrameBytes();
        var targets = _subscriptions.Values
            .Where(s => s.AppId == appId && s.Environment == environment)
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
                // 写入失败时由连接处理循环负责清理断开的连接
            }
        }
    }

    /// <summary>单条订阅记录</summary>
    /// <param name="AppId">应用标识</param>
    /// <param name="Environment">环境名称</param>
    /// <param name="Stream">网络流</param>
    private sealed record SubscriptionEntry(string AppId, string Environment, NetworkStream Stream);
}

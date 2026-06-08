namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 配置发布后刷新缓存并向 TCP 订阅方广播。
/// </summary>
public sealed class ConfigChangeNotifier(
    PublishedConfigCache cache,
    ConfigSubscriptionHub subscriptionHub)
{
    /// <summary>
    /// 刷新已发布快照并广播 <c>configChanged</c>。
    /// </summary>
    public async Task NotifyPublishedAsync(
        string appId,
        long releaseId,
        int fallbackVersion,
        CancellationToken cancellationToken = default)
    {
        // 刷新必须先于广播，客户端收到 configChanged 后立即拉取时才能读到最新快照。
        await cache.RefreshAsync(appId, releaseId > 0 ? releaseId : null, cancellationToken);
        var snapshot = await cache.GetOrLoadAsync(appId, cancellationToken: cancellationToken);
        // 若刷新后仍无法取到快照，使用调用方给出的版本兜底，保证广播消息可追踪。
        var version = snapshot?.Version ?? fallbackVersion;
        await subscriptionHub.BroadcastConfigChangedAsync(appId, version, cancellationToken);
    }
}

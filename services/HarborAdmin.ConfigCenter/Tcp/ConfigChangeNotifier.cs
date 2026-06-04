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
        await cache.RefreshAsync(appId, releaseId > 0 ? releaseId : null, cancellationToken);
        var snapshot = await cache.GetOrLoadAsync(appId, cancellationToken: cancellationToken);
        var version = snapshot?.Version ?? fallbackVersion;
        await subscriptionHub.BroadcastConfigChangedAsync(appId, version, cancellationToken);
    }
}

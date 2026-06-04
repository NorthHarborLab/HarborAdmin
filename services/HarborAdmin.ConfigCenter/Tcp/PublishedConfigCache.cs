using HarborAdmin.Modules.ConfigCenter.Application.Services;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 已发布配置快照的进程内内存缓存(ConfigCenter TCP 服务读路径使用)。
/// </summary>
/// <param name="memoryCache">内存缓存</param>
/// <param name="configCenterService">用于缓存未命中时从数据库加载</param>
public sealed class PublishedConfigCache(
    IMemoryCache memoryCache,
    ConfigCenterService configCenterService)
{
    private static string CacheKey(string appId) => $"config:{appId}";

    /// <summary>
    /// 获取已发布快照,<paramref name="version"/> 为 0 时优先读缓存中的最新版本。
    /// </summary>
    public async Task<PublishedConfigSnapshot?> GetOrLoadAsync(
        string appId,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        if (version > 0)
        {
            return await configCenterService.GetPublishedSnapshotAsync(appId, version, cancellationToken);
        }

        var key = CacheKey(appId);
        if (memoryCache.TryGetValue<PublishedConfigSnapshot>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await configCenterService.GetPublishedSnapshotAsync(appId, 0, cancellationToken);
        if (snapshot is not null)
        {
            memoryCache.Set(key, snapshot);
        }

        return snapshot;
    }

    /// <summary>
    /// 从数据库重新加载并更新缓存(通常在收到 <c>publishNotify</c> 后调用)。
    /// </summary>
    public async Task RefreshAsync(
        string appId,
        long? releaseId = null,
        CancellationToken cancellationToken = default)
    {
        PublishedConfigSnapshot? snapshot;
        if (releaseId.HasValue)
        {
            snapshot = await configCenterService.GetPublishedSnapshotByReleaseIdAsync(releaseId.Value, cancellationToken);
        }
        else
        {
            snapshot = await configCenterService.GetPublishedSnapshotAsync(appId, 0, cancellationToken);
        }

        if (snapshot is not null)
        {
            memoryCache.Set(CacheKey(appId), snapshot);
        }
        else
        {
            memoryCache.Remove(CacheKey(appId));
        }
    }
}

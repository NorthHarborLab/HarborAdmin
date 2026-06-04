using HarborAdmin.Modules.ConfigCenter.Application.Services;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 已发布配置快照的进程内内存缓存(ConfigCenter TCP 服务读路径使用)
/// </summary>
/// <param name="memoryCache">内存缓存</param>
/// <param name="configCenterService">用于缓存未命中时从数据库加载</param>
public sealed class PublishedConfigCache(
    IMemoryCache memoryCache,
    ConfigCenterService configCenterService)
{
    /// <summary>生成缓存键</summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    private static string CacheKey(string appId, string environment) => $"config:{appId}:{environment}";

    /// <summary>
    /// 获取已发布快照,<paramref name="version"/> 为 0 时优先读缓存中的最新版本
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="version">指定版本号,0 表示最新</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<PublishedConfigSnapshot?> GetOrLoadAsync(
        string appId,
        string environment,
        int version = 0,
        CancellationToken cancellationToken = default)
    {
        if (version > 0)
        {
            return await configCenterService.GetPublishedSnapshotAsync(appId, environment, version, cancellationToken);
        }

        var key = CacheKey(appId, environment);
        if (memoryCache.TryGetValue<PublishedConfigSnapshot>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await configCenterService.GetPublishedSnapshotAsync(appId, environment, 0, cancellationToken);
        if (snapshot is not null)
        {
            memoryCache.Set(key, snapshot);
        }

        return snapshot;
    }

    /// <summary>
    /// 从数据库重新加载并更新缓存(通常在收到 <c>publishNotify</c> 后调用)
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="releaseId">可选:按指定发布主键加载;为 <see langword="null"/> 时加载最新</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RefreshAsync(
        string appId,
        string environment,
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
            snapshot = await configCenterService.GetPublishedSnapshotAsync(appId, environment, 0, cancellationToken);
        }

        if (snapshot is not null)
        {
            memoryCache.Set(CacheKey(appId, environment), snapshot);
        }
        else
        {
            memoryCache.Remove(CacheKey(appId, environment));
        }
    }
}

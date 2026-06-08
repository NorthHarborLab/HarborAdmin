using HarborAdmin.Modules.ConfigCenter.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Dto;

namespace HarborAdmin.ConfigCenter.Tcp;

/// <summary>
/// 已发布配置快照的进程内内存缓存(ConfigCenter TCP 服务读路径使用)。
/// </summary>
/// <param name="memoryCache">内存缓存</param>
/// <param name="snapshotService">用于缓存未命中时从数据库加载</param>
public sealed class PublishedConfigCache(
    IMemoryCache memoryCache,
    ConfigCenterSnapshotService snapshotService)
{
    /// <summary>
    /// 生成应用最新快照缓存键。
    /// </summary>
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
            // 指定版本用于回放或诊断，不能覆盖最新版本缓存。
            return await snapshotService.GetResolvedPublishedSnapshotAsync(appId, version, cancellationToken);
        }

        var key = CacheKey(appId);
        if (memoryCache.TryGetValue<PublishedConfigSnapshot>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await snapshotService.GetResolvedPublishedSnapshotAsync(appId, 0, cancellationToken);
        if (snapshot is not null)
        {
            // 只缓存最新 resolved 快照；Secret 明文仅存在于进程内内存，不写回数据库。
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
            // Host 发布通知携带 releaseId 时，按发布主键读取可避免并发发布下的版本选择歧义。
            snapshot = await snapshotService.GetResolvedPublishedSnapshotByReleaseIdAsync(releaseId.Value, cancellationToken);
        }
        else
        {
            snapshot = await snapshotService.GetResolvedPublishedSnapshotAsync(appId, 0, cancellationToken);
        }

        if (snapshot is not null)
        {
            memoryCache.Set(CacheKey(appId), snapshot);
        }
        else
        {
            // 数据库没有发布快照时清掉旧缓存，避免继续向客户端返回过期配置。
            memoryCache.Remove(CacheKey(appId));
        }
    }
}

using System.Text.Json;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// AIWorker 发布配置快照缓存。
/// </summary>
public sealed class AiRuntimeConfigCache(IServiceScopeFactory scopeFactory, ILogger<AiRuntimeConfigCache> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private AiConfigSnapshot? _current;
    
    /// <summary>
    /// 获取当前快照，首次调用时自动加载最新发布。
    /// </summary>
    public async Task<AiConfigSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_current is not null)
        {
            return _current;
        }

        return await ReloadLatestAsync(cancellationToken);
    }

    /// <summary>
    /// 加载最新发布快照。
    /// </summary>
    public async Task<AiConfigSnapshot?> ReloadLatestAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAiReleaseRepository>();
        var release = await repository.GetLatestReleaseAsync(cancellationToken);
        if (release is null)
        {
            return null;
        }

        return await LoadReleaseAsync(release.Id, cancellationToken);
    }

    /// <summary>
    /// 按发布 ID 热加载快照；失败时保持旧快照。
    /// </summary>
    public async Task<AiConfigSnapshot?> LoadReleaseAsync(long releaseId, CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAiReleaseRepository>();
            var release = await repository.GetReleaseAsync(releaseId, cancellationToken);
            if (release is null)
            {
                logger.LogWarning("AI config release {ReleaseId} was not found.", releaseId);
                return _current;
            }

            // 快照反序列化成功后才替换 _current；任何异常都会保留旧配置继续服务。
            var snapshot = JsonSerializer.Deserialize<AiConfigSnapshot>(release.SnapshotJson, JsonOptions);
            if (snapshot is null)
            {
                logger.LogWarning("AI config release {ReleaseId} contains empty snapshot.", releaseId);
                return _current;
            }

            _current = snapshot;
            logger.LogInformation("AI config release {ReleaseVersion} loaded.", snapshot.Version);
            return _current;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI config release {ReleaseId} failed to load; old release remains active.", releaseId);
            return _current;
        }
        finally
        {
            _reloadLock.Release();
        }
    }
}

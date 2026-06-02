using System.Collections.Concurrent;
using System.Data;
using FreeSql;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// 多库工作单元管理器
/// </summary>
public sealed class UnitOfWorkManagerCloud : IDisposable
{
    private readonly ConcurrentDictionary<string, UnitOfWorkManager> _managers = new();
    private readonly HarborFreeSqlCloud _cloud;
    private int _disposeCounter;

    /// <summary>
    /// 创建实例。
    /// </summary>
    /// <param name="cloud">FreeSql 云</param>
    public UnitOfWorkManagerCloud(HarborFreeSqlCloud cloud)
    {
        _cloud = cloud;
    }

    /// <summary>
    /// 获取指定库的工作单元管理器
    /// </summary>
    public UnitOfWorkManager GetUnitOfWorkManager(string dbKey) =>
        _managers.GetOrAdd(dbKey, key => new UnitOfWorkManager(_cloud.Use(key)));

    /// <summary>
    /// 开启工作单元
    /// </summary>
    public IUnitOfWork Begin(string dbKey, Propagation propagation = Propagation.Required, IsolationLevel? isolationLevel = null) =>
        GetUnitOfWorkManager(dbKey).Begin(propagation, isolationLevel);

    /// <inheritdoc />
    public void Dispose()
    {
        // Dispose 可能被容器和调用方重复触发，这里保证每个 UnitOfWorkManager 只释放一次。
        if (Interlocked.Increment(ref _disposeCounter) != 1)
        {
            return;
        }

        Exception? ex = null;
        foreach (var uowm in _managers.Values)
        {
            try
            {
                uowm.Dispose();
            }
            catch (Exception e)
            {
                // 继续释放剩余管理器，最后再抛出最后一次失败，避免资源被提前中断。
                ex = e;
            }
        }

        _managers.Clear();
        if (ex is not null)
        {
            throw ex;
        }
    }
}

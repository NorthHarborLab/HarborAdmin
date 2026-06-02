using FreeSql;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// 多库 FreeSql 云实例
/// </summary>
public sealed class HarborFreeSqlCloud : FreeSqlCloud<string>
{
    /// <summary>
    /// 创建实例
    /// </summary>
    public HarborFreeSqlCloud() : base(null)
    {
    }

    /// <summary>
    /// 使用分布式锁键创建实例
    /// </summary>
    /// <param name="distributeKey">分布式锁键</param>
    public HarborFreeSqlCloud(string distributeKey) : base(distributeKey)
    {
    }
}

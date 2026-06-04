namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// FreeSql 结构同步前的数据库迁移钩子。
/// </summary>
public interface IHarborFreeSqlPreSyncHook
{
    /// <summary>
    /// 在指定数据库执行 CodeFirst SyncStructure 之前调用。
    /// </summary>
    /// <param name="freeSql">当前数据库连接。</param>
    /// <param name="dbKey">数据库 Key。</param>
    /// <param name="entityTypes">即将同步的实体类型。</param>
    void BeforeSyncStructure(IFreeSql freeSql, string dbKey, IReadOnlyList<Type> entityTypes);
}

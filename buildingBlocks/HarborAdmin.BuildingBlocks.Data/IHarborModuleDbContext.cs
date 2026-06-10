namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// Harbor 模块数据库上下文。
/// </summary>
public interface IHarborModuleDbContext
{
    /// <summary>
    /// 当前模块数据库 Key。
    /// </summary>
    string DbKey { get; }

    /// <summary>
    /// 当前模块 ORM。
    /// </summary>
    IFreeSql Orm { get; }

    /// <summary>
    /// 获取指定数据库 Key 的 ORM。
    /// </summary>
    /// <param name="dbKey">数据库 Key。</param>
    /// <returns>ORM。</returns>
    IFreeSql GetOrm(string dbKey);

    /// <summary>
    /// 在当前异步作用域绑定事务 ORM。
    /// </summary>
    /// <param name="orm">工作单元 ORM。</param>
    /// <returns>释放绑定的句柄。</returns>
    IDisposable Bind(IFreeSql orm);

    /// <summary>
    /// 在当前异步作用域为指定数据库 Key 绑定事务 ORM。
    /// </summary>
    /// <param name="dbKey">数据库 Key。</param>
    /// <param name="orm">工作单元 ORM。</param>
    /// <returns>释放绑定的句柄。</returns>
    IDisposable Bind(string dbKey, IFreeSql orm);
}
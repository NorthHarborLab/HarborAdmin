namespace HarborAdmin.Modules.AI.Infrastructure.Contexts;

/// <summary>
/// AI 模块数据库上下文。
/// </summary>
public interface IAiDbContext
{
    /// <summary>
    /// AI 模块 ORM。
    /// </summary>
    IFreeSql Orm { get; }

    /// <summary>
    /// 在当前异步作用域绑定事务 ORM。
    /// </summary>
    IDisposable Bind(IFreeSql orm);
}


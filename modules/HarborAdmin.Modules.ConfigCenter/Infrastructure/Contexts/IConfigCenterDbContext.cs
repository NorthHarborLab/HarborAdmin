using FreeSql;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

/// <summary>ConfigCenter 模块当前请求/事务内的 FreeSql 访问点。</summary>
public interface IConfigCenterDbContext
{
    /// <summary>当前 ORM（事务内为 <see cref="IUnitOfWork.Orm"/>）。</summary>
    IFreeSql Orm { get; }

    /// <summary>在作用域内绑定指定 ORM（用于 CAP + UoW 发布）。</summary>
    /// <param name="orm">工作单元 ORM。</param>
    /// <returns>释放绑定的句柄。</returns>
    IDisposable Bind(IFreeSql orm);
}

namespace HarborAdmin.Modules.Admin.Infrastructure.Contexts;

/// <summary>
/// Admin 模块数据库上下文。
/// </summary>
public interface IAdminDbContext
{
    /// <summary>
    /// 当前模块使用的 FreeSql 实例。
    /// </summary>
    IFreeSql Orm { get; }
}

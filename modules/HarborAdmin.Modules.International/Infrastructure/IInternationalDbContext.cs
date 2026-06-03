namespace HarborAdmin.Modules.International.Infrastructure;

/// <summary>
/// 国际化模块数据库上下文
/// </summary>
public interface IInternationalDbContext
{
    /// <summary>
    /// 当前模块使用的 FreeSql 实例
    /// </summary>
    IFreeSql Orm { get; }
}

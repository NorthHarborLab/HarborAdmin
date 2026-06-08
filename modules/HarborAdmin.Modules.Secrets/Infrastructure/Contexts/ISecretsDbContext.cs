namespace HarborAdmin.Modules.Secrets.Infrastructure.Contexts;

/// <summary>
/// Secrets 模块数据库上下文。
/// </summary>
public interface ISecretsDbContext
{
    /// <summary>
    /// 当前模块使用的 FreeSql 实例。
    /// </summary>
    IFreeSql Orm { get; }
}

using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Infrastructure.Contexts;

namespace HarborAdmin.Modules.International.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的国际化仓储实现。
/// </summary>
public sealed partial class FreeSqlInternationalRepository(IInternationalDbContext db) : IInternationalRepository
{
    /// <summary>
    /// 国际化模块使用的 FreeSql 实例。
    /// </summary>
    private IFreeSql FreeSql => db.Orm;
}

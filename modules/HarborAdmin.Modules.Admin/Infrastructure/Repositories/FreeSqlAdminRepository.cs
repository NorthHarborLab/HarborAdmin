using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 Admin 仓储实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository(IAdminDbContext db) : IAdminRepository
{
    /// <summary>
    /// Admin 模块 ORM 实例。
    /// </summary>
    private IFreeSql FreeSql => db.Orm;
}

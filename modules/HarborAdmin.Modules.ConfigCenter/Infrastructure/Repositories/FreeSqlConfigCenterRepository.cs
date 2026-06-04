using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 <see cref="IConfigCenterRepository"/> 实现。
/// </summary>
public sealed partial class FreeSqlConfigCenterRepository(IConfigCenterDbContext db) : IConfigCenterRepository
{
    private IFreeSql FreeSql => db.Orm;
}

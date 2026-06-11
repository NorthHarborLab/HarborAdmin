using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Infrastructure.Contexts;

namespace HarborAdmin.Modules.International.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的国际化仓储实现。
/// </summary>
public sealed partial class FreeSqlInternationalRepository(IInternationalDbContext db)
    : FreeSqlModuleRepository<IInternationalDbContext>(db), IInternationalRepository
{
}

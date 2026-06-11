using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Secrets.Application.Abstractions;
using HarborAdmin.Modules.Secrets.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Secrets.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 Secrets 仓储实现。
/// </summary>
public sealed partial class FreeSqlSecretsRepository(ISecretsDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlModuleRepository<ISecretsDbContext>(db), ISecretsRepository
{
}

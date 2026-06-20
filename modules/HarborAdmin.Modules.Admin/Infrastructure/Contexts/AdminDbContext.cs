using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.Modules.Admin.Infrastructure.Contexts;

/// <summary>
/// Admin 模块数据库上下文实现。
/// </summary>
public sealed class AdminDbContext(
    HarborFreeSqlCloud cloud,
    DbModuleRegistry moduleRegistry) : HarborModuleDbContext<AdminStartUp>(cloud, moduleRegistry), IAdminDbContext
{
}

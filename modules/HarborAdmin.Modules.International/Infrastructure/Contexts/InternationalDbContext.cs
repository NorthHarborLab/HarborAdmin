using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.International.Infrastructure.Contexts;

/// <summary>
/// 国际化模块数据库上下文实现。
/// </summary>
public sealed class InternationalDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<InternationalStartUp>(cloud, moduleRegistry), IInternationalDbContext
{
}

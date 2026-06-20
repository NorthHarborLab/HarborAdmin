using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

/// <inheritdoc cref="IConfigCenterDbContext"/>
public sealed class ConfigCenterDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<ConfigCenterStartUp>(cloud, moduleRegistry), IConfigCenterDbContext
{
}

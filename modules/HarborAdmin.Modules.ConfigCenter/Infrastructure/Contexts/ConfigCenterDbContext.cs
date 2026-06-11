using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

/// <inheritdoc cref="IConfigCenterDbContext"/>
public sealed class ConfigCenterDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<ConfigCenterStartUp>(cloud, moduleRegistry), IConfigCenterDbContext
{
}

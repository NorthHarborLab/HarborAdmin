using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.Modules.AI.Infrastructure.Contexts;

/// <inheritdoc cref="IAiDbContext"/>
public sealed class AiDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<AiStartUp>(cloud, moduleRegistry), IAiDbContext
{
}


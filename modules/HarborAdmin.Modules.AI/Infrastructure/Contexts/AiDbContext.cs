using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.AI.Infrastructure.Contexts;

/// <inheritdoc cref="IAiDbContext"/>
public sealed class AiDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<AiStartUp>(cloud, moduleRegistry), IAiDbContext
{
}


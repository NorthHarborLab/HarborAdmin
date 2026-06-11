using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.Secrets.Infrastructure.Contexts;

/// <summary>
/// Secrets 模块数据库上下文实现。
/// </summary>
public sealed class SecretsDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<SecretsStartUp>(cloud, moduleRegistry), ISecretsDbContext
{
}

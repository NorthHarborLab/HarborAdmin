using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Secrets.Domain.Entities;

namespace HarborAdmin.Modules.Secrets.Infrastructure.Contexts;

/// <summary>
/// Secrets 模块数据库上下文实现。
/// </summary>
public sealed class SecretsDbContext(HarborFreeSqlCloud cloud, DbEntityRegistry entityRegistry) : ISecretsDbContext
{
    /// <inheritdoc />
    public IFreeSql Orm => cloud.Use(entityRegistry.GetDbKey<HarborSecret>());
}

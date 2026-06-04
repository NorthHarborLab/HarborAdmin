using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Infrastructure.Contexts;

/// <summary>
/// 国际化模块数据库上下文实现。
/// </summary>
public sealed class InternationalDbContext(
    HarborFreeSqlCloud cloud,
    DbEntityRegistry entityRegistry) : IInternationalDbContext
{
    /// <inheritdoc />
    public IFreeSql Orm => cloud.Use(entityRegistry.GetDbKey<InternationalPage>());
}

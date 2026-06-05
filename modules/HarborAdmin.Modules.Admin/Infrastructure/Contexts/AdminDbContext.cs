using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Contexts;

/// <summary>
/// Admin 模块数据库上下文实现。
/// </summary>
public sealed class AdminDbContext(
    HarborFreeSqlCloud cloud,
    DbEntityRegistry entityRegistry) : IAdminDbContext
{
    /// <inheritdoc />
    public IFreeSql Orm => cloud.Use(entityRegistry.GetDbKey<AdminResource>());
}

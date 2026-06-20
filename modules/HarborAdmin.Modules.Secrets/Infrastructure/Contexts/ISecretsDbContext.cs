using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.Modules.Secrets.Infrastructure.Contexts;

/// <summary>
/// Secrets 模块数据库上下文。
/// </summary>
public interface ISecretsDbContext : IHarborModuleDbContext
{
}

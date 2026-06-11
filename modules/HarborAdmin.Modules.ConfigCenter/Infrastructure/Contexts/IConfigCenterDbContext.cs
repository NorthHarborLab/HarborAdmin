using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

/// <summary>
/// ConfigCenter 模块当前请求/事务内的 FreeSql 访问点。
/// </summary>
public interface IConfigCenterDbContext : IHarborModuleDbContext
{
}

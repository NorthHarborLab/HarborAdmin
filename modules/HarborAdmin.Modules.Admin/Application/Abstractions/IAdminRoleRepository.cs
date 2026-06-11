using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 角色实体 CRUD 仓储。
/// </summary>
public interface IAdminRoleRepository : IHarborCrudRepository<AdminRole>
{
    /// <summary>
    /// 按权限码解析功能动作。
    /// </summary>
    Task<AdminFeatureAction> GetFeatureActionByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按功能编码与字段名解析功能字段。
    /// </summary>
    Task<AdminFeatureField> GetFeatureFieldAsync(string featureCode, string fieldName, CancellationToken cancellationToken = default);
}

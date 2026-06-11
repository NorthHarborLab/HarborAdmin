using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 部门实体 CRUD 仓储。
/// </summary>
public interface IAdminDepartmentRepository : IHarborCrudRepository<AdminDepartment>
{
    /// <summary>
    /// 部门是否存在用户。
    /// </summary>
    Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计直属下级部门数量。
    /// </summary>
    Task<long> CountChildrenAsync(long parentId, CancellationToken cancellationToken = default);
}

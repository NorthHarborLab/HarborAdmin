using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Dept;

/// <summary>
/// 部门管理服务。
/// </summary>
public sealed class DeptService(
    AdminServiceContext context,
    IAdminRepository repository,
    IHarborMapper mapper)
{
    /// <summary>
    /// 获取部门树。
    /// </summary>
    public async Task<IReadOnlyList<SystemDeptDto>> ListDepartmentsAsync(CancellationToken cancellationToken)
    {
        var depts = await repository.ListDepartmentsAsync(cancellationToken);
        return BuildDepartmentTree(depts);
    }

    /// <summary>
    /// 新增或更新部门。
    /// </summary>
    public async Task<SystemDeptDto> SaveDepartmentAsync(long? id, SaveSystemDeptRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dept = id.HasValue
            ? await repository.GetDepartmentAsync(id.Value, cancellationToken)
              ?? throw new NotFoundDomainException("部门不存在。")
            : new AdminDepartment { CreatedAt = now, DeptCode = AdminIdHelper.BuildCode(request.Name) };
        dept.Name = request.Name;
        dept.ParentId = AdminIdHelper.ParseNullableId(request.Pid);
        dept.Remark = request.Remark;
        dept.Enabled = request.Status == 1;
        dept.UpdatedAt = now;

        if (id.HasValue)
        {
            await repository.UpdateDepartmentAsync(dept, cancellationToken);
        }
        else
        {
            await repository.InsertDepartmentAsync(dept, cancellationToken);
        }

        await context.BumpSessionVersionAsync(cancellationToken);
        return mapper.Map<SystemDeptDto>(dept);
    }

    /// <summary>
    /// 删除部门。
    /// </summary>
    public async Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken)
    {
        if (await repository.DepartmentHasUsersAsync(id, cancellationToken))
        {
            throw new ConflictDomainException("部门下存在用户，不能删除。");
        }

        await repository.DeleteDepartmentAsync(id, cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    private IReadOnlyList<SystemDeptDto> BuildDepartmentTree(IReadOnlyList<AdminDepartment> departments) =>
        departments
            .Where(dept => !dept.ParentId.HasValue)
            .Select(dept => ToDeptTreeNode(dept, departments))
            .ToArray();

    private SystemDeptDto ToDeptTreeNode(AdminDepartment dept, IReadOnlyList<AdminDepartment> departments)
    {
        var children = departments
            .Where(child => child.ParentId == dept.Id)
            .Select(child => ToDeptTreeNode(child, departments))
            .ToArray();
        var dto = mapper.Map<SystemDeptDto>(dept);
        return dto with { Children = children.Length > 0 ? children : null };
    }
}

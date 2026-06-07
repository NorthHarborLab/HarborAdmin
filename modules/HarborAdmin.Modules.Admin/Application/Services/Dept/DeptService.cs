using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.System;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Application.Services.Shared;

namespace HarborAdmin.Modules.Admin.Application.Services.Dept;

/// <summary>
/// 部门管理服务。
/// </summary>
public sealed class DeptService(AdminServiceContext context)
{
    private IFreeSql Orm => context.Orm;

    /// <summary>
    /// 获取部门树。
    /// </summary>
    public async Task<IReadOnlyList<SystemDeptDto>> ListDepartmentsAsync(CancellationToken cancellationToken)
    {
        var depts = await Orm.Select<AdminDepartment>()
            .OrderBy(dept => dept.Id)
            .ToListAsync(cancellationToken);
        return BuildDepartmentTree(depts);
    }

    /// <summary>
    /// 新增或更新部门。
    /// </summary>
    public async Task<SystemDeptDto> SaveDepartmentAsync(long? id, SaveSystemDeptRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dept = id.HasValue
            ? await Orm.Select<AdminDepartment>().Where(item => item.Id == id).ToOneAsync(cancellationToken)
              ?? throw new NotFoundDomainException("部门不存在。")
            : new AdminDepartment { CreatedAt = now, DeptCode = AdminIdHelper.BuildCode(request.Name) };
        dept.Name = request.Name;
        dept.ParentId = AdminIdHelper.ParseNullableId(request.Pid);
        dept.Remark = request.Remark;
        dept.Enabled = request.Status == 1;
        dept.UpdatedAt = now;

        if (id.HasValue)
        {
            await Orm.Update<AdminDepartment>().SetSource(dept).ExecuteAffrowsAsync(cancellationToken);
        }
        else
        {
            await Orm.Insert(dept).ExecuteAffrowsAsync(cancellationToken);
        }

        await context.BumpSessionVersionAsync(cancellationToken);
        return ToDeptDto(dept, []);
    }

    /// <summary>
    /// 删除部门。
    /// </summary>
    public async Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken)
    {
        var hasUser = await Orm.Select<AdminUser>().Where(user => user.DeptId == id).AnyAsync(cancellationToken);
        if (hasUser)
        {
            throw new ConflictDomainException("部门下存在用户，不能删除。");
        }

        await Orm.Delete<AdminDepartment>().Where(dept => dept.Id == id).ExecuteAffrowsAsync(cancellationToken);
        await context.BumpSessionVersionAsync(cancellationToken);
    }

    private static IReadOnlyList<SystemDeptDto> BuildDepartmentTree(IReadOnlyList<AdminDepartment> departments) =>
        departments
            .Where(dept => !dept.ParentId.HasValue)
            .Select(dept => ToDeptDto(dept, departments))
            .ToArray();

    private static SystemDeptDto ToDeptDto(AdminDepartment dept, IReadOnlyList<AdminDepartment> departments)
    {
        var children = departments
            .Where(child => child.ParentId == dept.Id)
            .Select(child => ToDeptDto(child, departments))
            .ToArray();
        return new SystemDeptDto(
            dept.Id.ToString(),
            dept.ParentId?.ToString() ?? "0",
            dept.Name,
            dept.Remark,
            dept.Enabled ? 1 : 0,
            children.Length > 0 ? children : null);
    }
}

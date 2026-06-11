using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Dept;

/// <summary>
/// 部门管理服务。
/// </summary>
public sealed class DeptService(
    AdminServiceContext context,
    IAdminDepartmentRepository repository,
    IHarborMapper mapper)
    : HarborApplicationRepositoryService<AdminDepartment, SystemDeptDto, SaveSystemDeptRequest, IAdminDepartmentRepository>(repository)
{
    private string? _deleteRejectedMessage;

    /// <summary>
    /// 获取部门树。
    /// </summary>
    public override async Task<IReadOnlyList<SystemDeptDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var depts = await Repository.ListAsync(cancellationToken);
        return BuildDepartmentTree(depts);
    }

    /// <inheritdoc />
    protected override SystemDeptDto MapToDto(AdminDepartment entity) => mapper.Map<SystemDeptDto>(entity);

    /// <inheritdoc />
    protected override AdminDepartment CreateEntity(SaveSystemDeptRequest request) =>
        new() { CreatedAt = UtcNow, DeptCode = AdminIdHelper.BuildCode(request.Name) };

    /// <summary>
    /// 将保存请求应用到部门。
    /// </summary>
    protected override async Task ApplySaveAsync(AdminDepartment entity, SaveSystemDeptRequest request, CancellationToken cancellationToken)
    {
        var parentId = AdminIdHelper.ParseNullableId(request.Pid);
        await EnsureValidParentAsync(entity.Id > 0 ? entity.Id : null, parentId, cancellationToken);
        entity.Name = request.Name;
        entity.ParentId = parentId;
        entity.Remark = request.Remark;
        entity.Enabled = request.Status == 1;
        entity.UpdatedAt = UtcNow;
    }

    /// <inheritdoc />
    protected override async Task<CrudDeleteDecision> CanDeleteAsync(AdminDepartment entity, CancellationToken cancellationToken)
    {
        if (await Repository.CountChildrenAsync(entity.Id, cancellationToken) > 0)
        {
            _deleteRejectedMessage = "请先删除下级部门。";
            return CrudDeleteDecision.Reject;
        }

        if (await Repository.HasUsersAsync(entity.Id, cancellationToken))
        {
            _deleteRejectedMessage = "部门下存在用户，不能删除。";
            return CrudDeleteDecision.Reject;
        }

        return CrudDeleteDecision.PhysicalDelete;
    }

    /// <inheritdoc />
    protected override async Task AfterSaveAsync(AdminDepartment entity, SaveSystemDeptRequest request, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override async Task AfterDeleteAsync(AdminDepartment entity, CrudDeleteDecision decision, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override string GetNotFoundMessage(long id) => "部门不存在。";

    /// <inheritdoc />
    protected override string GetDeleteRejectedMessage(AdminDepartment entity) => _deleteRejectedMessage ?? "部门不能删除。";

    /// <summary>
    /// 将部门列表构建为树形 DTO。
    /// </summary>
    private IReadOnlyList<SystemDeptDto> BuildDepartmentTree(IReadOnlyList<AdminDepartment> departments) =>
        departments
            .Where(dept => !dept.ParentId.HasValue)
            .Select(dept => ToDeptTreeNode(dept, departments))
            .ToArray();

    /// <summary>
    /// 递归构建单个部门树节点。
    /// </summary>
    private SystemDeptDto ToDeptTreeNode(AdminDepartment dept, IReadOnlyList<AdminDepartment> departments)
    {
        var children = departments
            .Where(child => child.ParentId == dept.Id)
            .Select(child => ToDeptTreeNode(child, departments))
            .ToArray();
        var dto = mapper.Map<SystemDeptDto>(dept);
        return dto with { Children = children.Length > 0 ? children : null };
    }

    /// <summary>
    /// 校验部门父级存在且不会形成循环。
    /// </summary>
    private async Task EnsureValidParentAsync(long? currentId, long? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        if (currentId == parentId)
        {
            throw new ValidationDomainException("上级部门不能选择当前部门。");
        }

        var departments = await Repository.ListAsync(cancellationToken);
        if (departments.All(dept => dept.Id != parentId.Value))
        {
            throw new NotFoundDomainException("上级部门不存在。");
        }

        if (!currentId.HasValue)
        {
            return;
        }

        var nextParentId = parentId;
        while (nextParentId.HasValue)
        {
            if (nextParentId.Value == currentId.Value)
            {
                throw new ValidationDomainException("上级部门不能选择当前部门的下级部门。");
            }

            nextParentId = departments.FirstOrDefault(dept => dept.Id == nextParentId.Value)?.ParentId;
        }
    }
}

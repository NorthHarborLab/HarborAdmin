using HarborAdmin.BuildingBlocks.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using HarborAdmin.Modules.Admin.Contracts.Shared.ErrorCode;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.Dept;

/// <summary>
/// 部门管理服务。
/// </summary>
public sealed class DeptService(
    AdminServiceContext context,
    IAdminDepartmentRepository repository,
    IHarborMapper mapper)
    : HarborCrudApplicationService<AdminDepartment, SystemDeptDto, PageRequest, SaveSystemDeptRequest, IAdminDepartmentRepository>(repository)
{
    /// <summary>
    /// 获取部门树。
    /// </summary>
    public override async Task<HarborResult<IReadOnlyList<SystemDeptDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var depts = await Repository.ListAsync(HarborQueryOptions.Empty, cancellationToken);
        return HarborResult<IReadOnlyList<SystemDeptDto>>.Success(BuildDepartmentTree(depts));
    }

    /// <inheritdoc />
    protected override SystemDeptDto MapToDto(AdminDepartment entity) => mapper.Map<SystemDeptDto>(entity);

    /// <inheritdoc />
    protected override AdminDepartment CreateEntity(SaveSystemDeptRequest request) =>
        new() { CreatedAt = UtcNow, DeptCode = AdminIdHelper.BuildCode(request.Name) };

    /// <summary>
    /// 将保存请求应用到部门。
    /// </summary>
    protected override async Task<HarborResult> ApplySaveAsync(
        AdminDepartment entity,
        SaveSystemDeptRequest request,
        CancellationToken cancellationToken)
    {
        var parentId = AdminIdHelper.ParseNullableId(request.Pid);
        var parentValidation = await ValidateParentAsync(entity.Id > 0 ? entity.Id : null, parentId, cancellationToken);
        if (!parentValidation.IsSuccess)
        {
            return parentValidation;
        }

        if (await Repository.DeptCodeExistsAsync(
                entity.DeptCode,
                entity.Id > 0 ? entity.Id : null,
                cancellationToken))
        {
            return HarborResult.Failure(AdminDepartmentErrorCodes.DuplicateCode.Create(
                new Dictionary<string, object?> { ["deptCode"] = entity.DeptCode }));
        }

        entity.Name = request.Name;
        entity.ParentId = parentId;
        entity.Remark = request.Remark;
        entity.Enabled = request.Status == 1;
        entity.UpdatedAt = UtcNow;
        return HarborResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<HarborResult<CrudDeleteDecision>> CanDeleteAsync(AdminDepartment entity, CancellationToken cancellationToken)
    {
        if (await Repository.CountChildrenAsync(entity.Id, cancellationToken) > 0)
        {
            return HarborResult<CrudDeleteDecision>.Failure(AdminDepartmentErrorCodes.HasChildren.Create(
                new Dictionary<string, object?> { ["id"] = entity.Id }));
        }

        if (await Repository.HasUsersAsync(entity.Id, cancellationToken))
        {
            return HarborResult<CrudDeleteDecision>.Failure(AdminDepartmentErrorCodes.HasUsers.Create(
                new Dictionary<string, object?> { ["id"] = entity.Id }));
        }

        return HarborResult<CrudDeleteDecision>.Success(CrudDeleteDecision.PhysicalDelete);
    }

    /// <inheritdoc />
    protected override async Task AfterSaveAsync(AdminDepartment entity, SaveSystemDeptRequest request, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override async Task AfterDeleteAsync(AdminDepartment entity, CrudDeleteDecision decision, CancellationToken cancellationToken) =>
        await context.BumpSessionVersionAsync(cancellationToken);

    /// <inheritdoc />
    protected override HarborErrorDefinition NotFoundError => AdminDepartmentErrorCodes.NotFound;

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
    private async Task<HarborResult> ValidateParentAsync(long? currentId, long? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return HarborResult.Success();
        }

        if (currentId == parentId)
        {
            return HarborResult.Failure(AdminDepartmentErrorCodes.InvalidParent.Create());
        }

        var departments = await Repository.ListAsync(HarborQueryOptions.Empty, cancellationToken);
        if (departments.All(dept => dept.Id != parentId.Value))
        {
            return HarborResult.Failure(AdminDepartmentErrorCodes.ParentNotFound.Create(
                new Dictionary<string, object?> { ["parentId"] = parentId.Value }));
        }

        if (!currentId.HasValue)
        {
            return HarborResult.Success();
        }

        var nextParentId = parentId;
        while (nextParentId.HasValue)
        {
            if (nextParentId.Value == currentId.Value)
            {
                return HarborResult.Failure(AdminDepartmentErrorCodes.InvalidParent.Create());
            }

            nextParentId = departments.FirstOrDefault(dept => dept.Id == nextParentId.Value)?.ParentId;
        }

        return HarborResult.Success();
    }
}

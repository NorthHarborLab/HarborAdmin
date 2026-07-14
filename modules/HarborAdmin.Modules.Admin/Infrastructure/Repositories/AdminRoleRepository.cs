using FreeSql;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.Repositories;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin 角色实体 CRUD 仓储。
/// </summary>
public sealed class AdminRoleRepository(IAdminDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlCrudRepository<AdminRole, IAdminDbContext>(db, entityRegistry, unitOfWorkManager), IAdminRoleRepository
{
    /// <inheritdoc />
    public Task<bool> RoleCodeExistsAsync(string roleCode, long? excludeId, CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<AdminRole>().Where(entity => entity.RoleCode == roleCode);
        if (excludeId.HasValue)
        {
            query = query.Where(entity => entity.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<AdminRole> InsertAsync(AdminRole entity, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await base.InsertAsync(entity, ct);
            await SaveChildrenAsync(entity, ct);
        }, cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public override async Task<AdminRole> UpdateAsync(AdminRole entity, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await base.UpdateAsync(entity, ct);
            await DeleteChildrenAsync(entity.Id, ct);
            await SaveChildrenAsync(entity, ct);
        }, cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await ExecuteInUnitOfWorkAsync(async ct =>
        {
            await DeleteChildrenAsync(id, ct);
            await FreeSql.Delete<AdminUserRole>().Where(link => link.RoleId == id).ExecuteAffrowsAsync(ct);
            await base.DeleteAsync(id, ct);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminFeatureAction> GetFeatureActionByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        var normalized = permissionCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("权限编码不能为空。");
        }

        return await FreeSql.Select<AdminFeatureAction>()
                   .Where(action => action.PermissionCode == normalized)
                   .ToOneAsync(cancellationToken)
               ?? throw new NotFoundDomainException($"权限编码 '{normalized}' 未找到对应动作。");
    }

    /// <inheritdoc />
    public async Task<AdminFeatureField> GetFeatureFieldAsync(string featureCode, string fieldName, CancellationToken cancellationToken = default)
    {
        var normalizedFeatureCode = featureCode.Trim();
        var normalizedFieldName = fieldName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedFeatureCode) || string.IsNullOrWhiteSpace(normalizedFieldName))
        {
            throw new ValidationDomainException("功能编码与字段名不能为空。");
        }

        return await FreeSql.Select<AdminFeatureField>()
                   .Where(field => field.FeatureCode == normalizedFeatureCode && field.FieldCode == normalizedFieldName)
                   .ToOneAsync(cancellationToken)
               ?? throw new NotFoundDomainException($"功能字段 '{normalizedFeatureCode}.{normalizedFieldName}' 不存在。");
    }

    /// <summary>
    /// 保存角色子集合。
    /// </summary>
    private async Task SaveChildrenAsync(AdminRole role, CancellationToken cancellationToken)
    {
        foreach (var roleMenu in role.RoleMenus)
        {
            roleMenu.RoleId = role.Id;
        }

        foreach (var rolePermission in role.RolePermissions)
        {
            rolePermission.RoleId = role.Id;
        }

        foreach (var fieldPermission in role.FieldPermissions)
        {
            fieldPermission.RoleId = role.Id;
        }

        foreach (var dataScope in role.DataScopes)
        {
            dataScope.RoleId = role.Id;
        }

        if (role.RoleMenus.Count > 0)
        {
            await FreeSql.Insert(role.RoleMenus).ExecuteAffrowsAsync(cancellationToken);
        }

        if (role.RolePermissions.Count > 0)
        {
            await FreeSql.Insert(role.RolePermissions).ExecuteAffrowsAsync(cancellationToken);
        }

        if (role.FieldPermissions.Count > 0)
        {
            await FreeSql.Insert(role.FieldPermissions).ExecuteAffrowsAsync(cancellationToken);
        }

        if (role.DataScopes.Count > 0)
        {
            await FreeSql.Insert(role.DataScopes).ExecuteAffrowsAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 删除角色子集合。
    /// </summary>
    private async Task DeleteChildrenAsync(long roleId, CancellationToken cancellationToken)
    {
        await FreeSql.Delete<AdminRoleMenu>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AdminRolePermission>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AdminRoleFieldPermission>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        await FreeSql.Delete<AdminRoleDataScope>().Where(link => link.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
    }
}

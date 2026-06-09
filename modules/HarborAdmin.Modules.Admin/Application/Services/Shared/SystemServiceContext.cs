using FreeSql;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Application.Services.Shared;

/// <summary>
/// Admin 系统管理服务上下文与共享能力。
/// </summary>
public sealed class SystemServiceContext(IAdminDbContext db, IAdminRepository repository, AdminServiceContext adminContext)
{
    /// <summary>
    /// Admin 共享服务上下文。
    /// </summary>
    public AdminServiceContext AdminContext { get; } = adminContext;

    /// <summary>
    /// Admin ORM 实例。
    /// </summary>
    public IFreeSql Orm => db.Orm;

    /// <summary>
    /// 加载用户聚合。
    /// </summary>
    public Task<AdminUser?> LoadUserAggregateAsync(long userId, CancellationToken cancellationToken) =>
        repository.GetUserAggregateAsync(userId, cancellationToken);

    /// <summary>
    /// 加载角色聚合。
    /// </summary>
    public async Task<AdminRole> LoadRoleAggregateAsync(long roleId, CancellationToken cancellationToken) =>
        await repository.GetRoleAggregateAsync(roleId, cancellationToken)
        ?? throw new NotFoundDomainException("角色不存在。");

    /// <summary>
    /// 获取启用级联保存的用户仓储。
    /// </summary>
    public IBaseRepository<AdminUser> GetUserRepository()
    {
        var userRepository = db.Orm.GetRepository<AdminUser>();
        userRepository.DbContextOptions.EnableCascadeSave = true;
        return userRepository;
    }

    /// <summary>
    /// 获取启用级联保存的角色仓储。
    /// </summary>
    public IBaseRepository<AdminRole> GetRoleRepository()
    {
        var roleRepository = db.Orm.GetRepository<AdminRole>();
        roleRepository.DbContextOptions.EnableCascadeSave = true;
        return roleRepository;
    }

    /// <summary>
    /// 获取启用级联保存的菜单仓储。
    /// </summary>
    public IBaseRepository<AdminMenu> GetMenuRepository()
    {
        var menuRepository = db.Orm.GetRepository<AdminMenu>();
        menuRepository.DbContextOptions.EnableCascadeSave = true;
        return menuRepository;
    }

    /// <summary>
    /// 保存用户子集合。
    /// </summary>
    public void SaveUserChildren(AdminUser user, string propertyName) =>
        GetUserRepository().SaveMany(user, propertyName);

    /// <summary>
    /// 保存角色子集合。
    /// </summary>
    public void SaveRoleChildren(AdminRole role, string propertyName) =>
        GetRoleRepository().SaveMany(role, propertyName);

    /// <summary>
    /// 按权限码解析功能动作。
    /// </summary>
    public async Task<AdminFeatureAction> ResolveFeatureActionByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken)
    {
        var normalized = permissionCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("权限编码不能为空。");
        }

        return await Orm.Select<AdminFeatureAction>()
                   .Where(action => action.PermissionCode == normalized)
                   .ToOneAsync(cancellationToken)
               ?? throw new NotFoundDomainException($"权限编码 '{normalized}' 未找到对应动作。");
    }

    /// <summary>
    /// 按功能编码与字段名解析功能字段。
    /// </summary>
    public async Task<AdminFeatureField> ResolveFeatureFieldAsync(string featureCode, string fieldName, CancellationToken cancellationToken)
    {
        var normalizedFeatureCode = featureCode.Trim();
        var normalizedFieldName = fieldName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedFeatureCode) || string.IsNullOrWhiteSpace(normalizedFieldName))
        {
            throw new ValidationDomainException("功能编码与字段名不能为空。");
        }

        return await Orm.Select<AdminFeatureField>()
                   .Where(field => field.FeatureCode == normalizedFeatureCode && field.FieldCode == normalizedFieldName)
                   .ToOneAsync(cancellationToken)
               ?? throw new NotFoundDomainException($"功能字段 '{normalizedFeatureCode}.{normalizedFieldName}' 不存在。");
    }

    /// <summary>
    /// 按功能编码解析功能。
    /// </summary>
    public async Task<AdminFeature?> ResolveFeatureByCodeAsync(string? featureCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return null;
        }

        return await Orm.Select<AdminFeature>()
            .Where(feature => feature.FeatureCode == featureCode.Trim()
                              && feature.NodeType != AdminFeatureNodeType.Category)
            .ToOneAsync(cancellationToken);
    }
}

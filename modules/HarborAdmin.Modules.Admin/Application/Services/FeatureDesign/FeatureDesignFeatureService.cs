using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

public sealed class FeatureDesignFeatureService
{
    private readonly FeatureDesignServiceContext _context;

    /// <summary>
    /// 初始化功能页面服务。
    /// </summary>
    public FeatureDesignFeatureService(FeatureDesignServiceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 查询 Feature 列表。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureDto>> ListFeaturesAsync(CancellationToken cancellationToken)
    {
        var features = await _context.Db.Orm.Select<AdminFeature>().OrderBy(item => item.FeatureCode).ToListAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureDto[]>(features);
    }

    /// <summary>
    /// 新建 Feature。
    /// </summary>
    public async Task<AdminFeatureDto> CreateFeatureAsync(SaveAdminFeatureRequest request, CancellationToken cancellationToken)
    {
        var featureCode = request.FeatureCode.Trim();
        if (await _context.Db.Orm.Select<AdminFeature>().Where(item => item.FeatureCode == featureCode).AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException($"Feature '{featureCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var feature = new AdminFeature
        {
            FeatureCode = featureCode,
            CreatedAt = now,
        };
        ApplyFeature(feature, request, now);
        var repository = _context.GetFeatureRepository();
        repository.DbContextOptions.EnableCascadeSave = true;
        await repository.InsertAsync(feature, cancellationToken);
        await _context.BumpSessionVersionAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureDto>(feature);
    }

    /// <summary>
    /// 更新 Feature。
    /// </summary>
    public async Task<AdminFeatureDto> UpdateFeatureAsync(string featureCode, SaveAdminFeatureRequest request, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        var feature = await _context.Db.Orm.Select<AdminFeature>().Where(item => item.FeatureCode == normalized).ToOneAsync(cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
        ApplyFeature(feature, request, DateTimeOffset.UtcNow);
        await _context.GetFeatureRepository().UpdateAsync(feature, cancellationToken);
        await _context.BumpSessionVersionAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureDto>(feature);
    }

    /// <summary>
    /// 删除 Feature。
    /// </summary>
    public async Task DeleteFeatureAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        var feature = await _context.LoadFeatureAggregateAsync(normalized, cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
        var usedByMenu = await _context.Db.Orm.Select<AdminMenu>().Where(menu => menu.FeatureCode == normalized).AnyAsync(cancellationToken);
        if (usedByMenu)
        {
            throw new ConflictDomainException("功能已被菜单引用，不能删除。");
        }

        var permissionCodes = feature.Actions.Select(item => item.PermissionCode).ToArray();
        if (permissionCodes.Length > 0)
        {
            await _context.Db.Orm.Delete<AdminRolePermission>().Where(item => permissionCodes.Contains(item.PermissionCode)).ExecuteAffrowsAsync(cancellationToken);
        }

        await _context.Db.Orm.Delete<AdminRoleFieldPermission>().Where(item => item.FeatureCode == normalized).ExecuteAffrowsAsync(cancellationToken);
        await _context.GetFeatureRepository().DeleteCascadeByDatabaseAsync(item => item.Id == feature.Id, cancellationToken);
        await _context.BumpSessionVersionAsync(cancellationToken);
    }

    private static void ApplyFeature(AdminFeature feature, SaveAdminFeatureRequest request, DateTimeOffset now)
    {
        feature.FeatureCode = request.FeatureCode.Trim();
        feature.NameKey = request.NameKey.Trim();
        feature.NameFallback = string.IsNullOrWhiteSpace(request.NameFallback) ? null : request.NameFallback.Trim();
        if (!Enum.TryParse<AdminFeatureType>(request.FeatureType.Trim(), true, out var featureType))
        {
            throw new ValidationDomainException("功能类型不支持，仅支持 Static 或 Dynamic。");
        }

        feature.FeatureType = featureType.ToString();
        feature.Component = request.Component.Trim();
        feature.HandlerKey = string.IsNullOrWhiteSpace(request.HandlerKey) ? null : request.HandlerKey.Trim();
        feature.ModuleName = string.IsNullOrWhiteSpace(request.ModuleName) ? null : request.ModuleName.Trim();
        feature.RoutePath = string.IsNullOrWhiteSpace(request.RoutePath) ? null : request.RoutePath.Trim();
        feature.Enabled = request.Enabled;
        feature.UpdatedAt = now;
    }
}

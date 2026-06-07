using FreeSql;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

/// <summary>
/// Admin 功能设计服务上下文与共享能力。
/// </summary>
public sealed class FeatureDesignServiceContext
{
    public IHarborMapper Mapper { get; }
    public readonly IAdminDbContext Db;

    /// <summary>
    /// 初始化功能设计服务上下文。
    /// </summary>
    public FeatureDesignServiceContext(IAdminDbContext db, IHarborMapper mapper, AdminServiceContext adminContext)
    {
        Db = db;
        Mapper = mapper;
        AdminContext = adminContext;
    }

    /// <summary>
    /// Admin 共享服务上下文。
    /// </summary>
    public AdminServiceContext AdminContext { get; }


    public async Task<AdminFeatureAction> LoadActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        var normalizedAction = actionCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("Feature code is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedAction))
        {
            throw new ValidationDomainException("Action code is required.");
        }

        return await Db.Orm.Select<AdminFeatureAction>()
            .Where(item => item.FeatureCode == normalized && item.ActionCode == normalizedAction)
            .IncludeMany(item => item.ActionApis)
            .ToOneAsync(cancellationToken)
               ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
    }

    public async Task<AdminFeature?> LoadFeatureAggregateAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("Feature code is required.");
        }

        return await Db.Orm.Select<AdminFeature>()
            .Where(item => item.FeatureCode == normalized)
            .IncludeMany(item => item.Fields)
            .IncludeMany(item => item.Apis)
            .IncludeMany(item => item.Actions, then => then.IncludeMany(action => action.ActionApis))
            .ToOneAsync(cancellationToken);
    }

    public IBaseRepository<AdminFeature> GetFeatureRepository()
    {
        var repository = Db.Orm.GetRepository<AdminFeature>();
        repository.DbContextOptions.EnableCascadeSave = true;
        return repository;
    }

    public void SaveFeatureChildren(AdminFeature feature, string propertyName) =>
        GetFeatureRepository().SaveMany(feature, propertyName);

    public void SaveActionChildren(AdminFeatureAction action, string propertyName)
    {
        var repository = Db.Orm.GetRepository<AdminFeatureAction>();
        repository.DbContextOptions.EnableCascadeSave = true;
        repository.SaveMany(action, propertyName);
    }

    public async Task IncrementSchemaVersionAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("Feature code is required.");
        }

        var feature = await Db.Orm.Select<AdminFeature>().Where(item => item.FeatureCode == normalized).ToOneAsync(cancellationToken)
                      ?? throw new NotFoundDomainException($"Feature '{normalized}' was not found.");
        feature.SchemaVersion++;
        feature.UpdatedAt = DateTimeOffset.UtcNow;
        await Db.Orm.Update<AdminFeature>().SetSource(feature).ExecuteAffrowsAsync(cancellationToken);
    }

    public Task BumpSessionVersionAsync(CancellationToken cancellationToken) =>
        AdminContext.BumpSessionVersionAsync(cancellationToken);
}

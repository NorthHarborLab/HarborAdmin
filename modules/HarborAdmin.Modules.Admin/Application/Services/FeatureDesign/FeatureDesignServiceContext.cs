using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

/// <summary>
/// Admin 功能设计服务上下文与共享能力。
/// </summary>
public sealed class FeatureDesignServiceContext
{
    public IHarborMapper Mapper { get; }

    /// <summary>
    /// 初始化功能设计服务上下文。
    /// </summary>
    public FeatureDesignServiceContext(IHarborMapper mapper, AdminServiceContext adminContext, IAdminFeatureDesignRepository repository)
    {
        Mapper = mapper;
        AdminContext = adminContext;
        Repository = repository;
    }

    /// <summary>
    /// Admin 共享服务上下文。
    /// </summary>
    public AdminServiceContext AdminContext { get; }

    /// <summary>
    /// Admin 聚合仓储。
    /// </summary>
    public IAdminFeatureDesignRepository Repository { get; }

    public async Task<AdminFeatureAction> LoadActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        var normalizedAction = actionCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("功能编码不能为空。");
        }

        if (string.IsNullOrWhiteSpace(normalizedAction))
        {
            throw new ValidationDomainException("动作编码不能为空。");
        }

        return await Repository.GetFeatureActionAsync(normalized, normalizedAction, cancellationToken)
               ?? throw new NotFoundDomainException($"Feature action '{normalized}.{normalizedAction}' was not found.");
    }

    public async Task<AdminFeature?> LoadFeatureAggregateAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("功能编码不能为空。");
        }

        return await Repository.GetFeatureAggregateAsync(normalized, cancellationToken);
    }

    public AdminFeature EnsureFeatureNode(AdminFeature feature)
    {
        if (feature.NodeType == AdminFeatureNodeType.Category)
        {
            throw new ValidationDomainException("分类节点不能维护字段、API 或权限点。");
        }

        return feature;
    }

    public void SaveFeatureChildren(AdminFeature feature, string propertyName) =>
        Repository.SaveFeatureChildren(feature, propertyName);

    public void SaveActionChildren(AdminFeatureAction action, string propertyName) =>
        Repository.SaveActionChildren(action, propertyName);

    public async Task IncrementSchemaVersionAsync(string featureCode, CancellationToken cancellationToken)
    {
        var normalized = featureCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationDomainException("功能编码不能为空。");
        }

        await Repository.IncrementFeatureSchemaVersionAsync(normalized, cancellationToken);
    }
}

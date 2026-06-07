using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.Modules.Admin.Domain.Entities;
using Mapster;

namespace HarborAdmin.Modules.Admin.Application.Mappings;

/// <summary>
/// 功能设计 Mapster 映射配置。
/// </summary>
public sealed class AdminFeatureDesignMappingRegister : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AdminFeature, AdminFeatureDto>();
        config.NewConfig<AdminFeatureField, AdminFeatureFieldDto>();
        config.NewConfig<AdminFeatureApi, AdminFeatureApiDto>();
        config.NewConfig<AdminFeatureActionMappingSource, AdminFeatureActionDto>()
            .Map(destination => destination.Id, source => source.Action.Id)
            .Map(destination => destination.FeatureCode, source => source.Action.FeatureCode)
            .Map(destination => destination.ActionCode, source => source.Action.ActionCode)
            .Map(destination => destination.PermissionCode, source => source.Action.PermissionCode)
            .Map(destination => destination.LabelKey, source => source.Action.LabelKey)
            .Map(destination => destination.LabelFallback, source => source.Action.LabelFallback)
            .Map(destination => destination.SortOrder, source => source.Action.SortOrder)
            .Map(destination => destination.Enabled, source => source.Action.Enabled)
            .Map(destination => destination.ApiIds, source => source.ApiIds)
            .Map(destination => destination.CreatedAt, source => source.Action.CreatedAt)
            .Map(destination => destination.UpdatedAt, source => source.Action.UpdatedAt);
    }
}

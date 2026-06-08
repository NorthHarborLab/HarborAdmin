using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;
using Mapster;

namespace HarborAdmin.Modules.ConfigCenter.Application.Mappings;

/// <summary>
/// 配置中心 Mapster 映射配置。
/// </summary>
public sealed class ConfigCenterMappingRegister : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ConfigApplication, ConfigApplicationDto>();
        config.NewConfig<ConfigItem, ConfigItemDto>();
        config.NewConfig<ConfigRelease, ConfigReleaseDto>();
    }
}

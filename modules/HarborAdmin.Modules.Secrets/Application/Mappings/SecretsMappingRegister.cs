using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Modules.Secrets.Contracts.Dtos;
using Mapster;

namespace HarborAdmin.Modules.Secrets.Application.Mappings;

/// <summary>
/// Secrets 模块 Mapster 映射配置。
/// </summary>
public sealed class SecretsMappingRegister : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SecretDescriptor, SecretDto>();
    }
}

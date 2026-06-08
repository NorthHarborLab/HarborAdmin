using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Modules.Secrets.Contracts.Secret.Dto;
using HarborAdmin.Modules.Secrets.Domain.Entities;
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
        config.NewConfig<HarborSecret, SecretDto>()
            .Map(dest => dest.SecretConfigured, src => !string.IsNullOrWhiteSpace(src.CipherText))
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt ?? src.CreatedAt);
        config.NewConfig<SecretDescriptor, SecretDto>();
    }
}

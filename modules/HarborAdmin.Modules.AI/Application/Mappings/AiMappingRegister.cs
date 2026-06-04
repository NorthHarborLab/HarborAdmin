using Mapster;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Mappings;

/// <summary>
/// AI 模块 Mapster 映射配置。
/// </summary>
public sealed class AiMappingRegister : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AiProvider, AiProviderDto>()
            .Map(destination => destination.SecretConfigured,
                source => !string.IsNullOrWhiteSpace(source.SecretRef) && source.SecretVersion > 0)
            .Map(destination => destination.Models,
                source => source.Models.OrderBy(model => model.SortOrder).ThenBy(model => model.ModelName));

        config.NewConfig<AiBusiness, AiBusinessDto>()
            .Map(destination => destination.Routes,
                source => source.Routes.OrderBy(route => route.Priority));

        config.NewConfig<SecretDescriptor, AiSecretDto>();

        config.NewConfig<AiQuotaBucket, AiUsageLedgerDto>()
            .Map(destination => destination.RequestCount,
                source => source.ReservedRequests + source.SuccessRequests + source.FailedRequests);
    }
}

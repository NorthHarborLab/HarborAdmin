using HarborAdmin.Modules.AI.Contracts.Observability.Dto;
using HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;
using Mapster;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Contracts.Business.Dto;
using HarborAdmin.Modules.AI.Contracts.Provider.Dto;

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

        config.NewConfig<AiQuotaBucket, AiUsageLedgerDto>()
            .Map(destination => destination.RequestCount,
                source => source.ReservedRequests + source.SuccessRequests + source.FailedRequests);

        config.NewConfig<AiProviderModel, AiProviderModelSnapshot>();
        config.NewConfig<AiBusinessProviderRoute, AiBusinessRouteSnapshot>();
        config.NewConfig<AiPrompt, AiPromptSnapshot>();
        config.NewConfig<AiKnowledgeBase, AiKnowledgeSnapshot>();
        config.NewConfig<AiModelQuota, AiModelQuotaSnapshot>();

        config.NewConfig<AiProvider, AiProviderSnapshot>()
            .Map(destination => destination.Models,
                source => source.Models.Where(model => model.Enabled).OrderBy(model => model.SortOrder));

        config.NewConfig<AiBusiness, AiBusinessSnapshot>()
            .Map(destination => destination.Routes,
                source => source.Routes.Where(route => route.Enabled).OrderBy(route => route.Priority));

        config.NewConfig<AiProviderQuotaSnapshotSource, AiProviderQuotaSnapshot>()
            .Map(destination => destination.ProviderKey, source => source.ProviderKey)
            .Map(destination => destination.ProducerKey, source => source.Quota.ProducerKey)
            .Map(destination => destination.RequestsPerMinute, source => source.Quota.RequestsPerMinute)
            .Map(destination => destination.RequestsPerDay, source => source.Quota.RequestsPerDay)
            .Map(destination => destination.TokensPerDay, source => source.Quota.TokensPerDay)
            .Map(destination => destination.TokensPerMonth, source => source.Quota.TokensPerMonth)
            .Map(destination => destination.MonthlyBudget, source => source.Quota.MonthlyBudget);
    }
}

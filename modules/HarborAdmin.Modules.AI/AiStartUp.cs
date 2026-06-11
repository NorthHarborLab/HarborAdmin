using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Application.Services.Business;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Modules.AI.Application.Services.Conversation;
using HarborAdmin.Modules.AI.Application.Services.KnowledgeBase;
using HarborAdmin.Modules.AI.Application.Services.Observability;
using HarborAdmin.Modules.AI.Application.Services.Prompt;
using HarborAdmin.Modules.AI.Application.Services.Provider;
using HarborAdmin.Modules.AI.Application.Services.Quota;
using HarborAdmin.Modules.AI.Application.Services.Release;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;
using HarborAdmin.Modules.AI.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.AI;

/// <summary>
/// AI 模块启动入口。
/// </summary>
public sealed class AiStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    /// <inheritdoc />
    public override string ModuleName => "AI";

    /// <inheritdoc />
    public override string GetDbKey() => "AdminDb";

    /// <summary>
    /// 注册 AI 模块。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="context">模块注册上下文。</param>
    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        services.AddSingleton<IAiDbContext, AiDbContext>();
        services.AddScoped<IAiConversationRepository, AiConversationRepository>();
        services.AddScoped<IAiInvocationRepository, AiInvocationRepository>();
        services.AddScoped<IAiQuotaRepository, AiQuotaRepository>();
        services.AddScoped<IAiReleaseRepository, AiReleaseRepository>();
        services.AddScoped<IAiProviderRepository, AiProviderRepository>();
        services.AddScoped<IAiBusinessRepository, AiBusinessRepository>();
        services.AddScoped<IAiPromptRepository, AiPromptRepository>();
        services.AddScoped<IAiKnowledgeBaseRepository, AiKnowledgeBaseRepository>();
        services.AddScoped<IAiModelQuotaRepository, AiModelQuotaRepository>();
        // AIWorker 只执行模型请求，不注册管理端 CRUD 服务，避免依赖 Host 侧 SecretStore 等能力。
        if (!context.IsHostKind(HarborHostKinds.AIWorker))
        {
            services.AddScoped<ProviderService>();
            services.AddScoped<BusinessService>();
            services.AddScoped<PromptService>();
            services.AddScoped<KnowledgeBaseService>();
            services.AddScoped<QuotaService>();
            services.AddScoped<ReleaseService>();
            services.AddScoped<AiObservabilityService>();
            services.AddScoped<AiChatStreamService>();
            services.AddScoped<ConversationService>();
            services.AddScoped<IAiBusinessSigningSecretResolver, AiBusinessSigningSecretResolver>();
        }
    }
}

using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Application.Services.Business;
using HarborAdmin.Modules.AI.Application.Services.KnowledgeBase;
using HarborAdmin.Modules.AI.Application.Services.Observability;
using HarborAdmin.Modules.AI.Application.Services.Prompt;
using HarborAdmin.Modules.AI.Application.Services.Provider;
using HarborAdmin.Modules.AI.Application.Services.Quota;
using HarborAdmin.Modules.AI.Application.Services.Release;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;
using HarborAdmin.Modules.AI.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.AI;

/// <summary>
/// AI 模块依赖注入扩展。
/// </summary>
public static class AiModuleExtensions
{
    /// <summary>
    /// 注册 AI 模块。
    /// </summary>
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAiDbContext, AiDbContext>();
        services.AddSingleton<IAiRepository, FreeSqlAiRepository>();
        services.AddScoped<AiServiceContext>();
        services.AddScoped<ProviderService>();
        services.AddScoped<BusinessService>();
        services.AddScoped<PromptService>();
        services.AddScoped<KnowledgeBaseService>();
        services.AddScoped<QuotaService>();
        services.AddScoped<ReleaseService>();
        services.AddScoped<AiObservabilityService>();
        services.AddScoped<AiChatStreamService>();
        return services;
    }
}

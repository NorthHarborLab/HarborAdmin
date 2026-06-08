using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Application.Abstractions;
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
        services.AddScoped<AiManagementService>();
        services.AddScoped<AiChatStreamService>();
        return services;
    }
}


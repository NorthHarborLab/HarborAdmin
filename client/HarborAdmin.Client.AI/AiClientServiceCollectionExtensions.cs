using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Client.AI;

/// <summary>
/// AIWorker 客户端依赖注入扩展。
/// </summary>
public static class AiClientServiceCollectionExtensions
{
    /// <summary>
    /// 注册 AIWorker 调用客户端。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置</param>
    /// <returns>原服务集合</returns>
    public static IServiceCollection AddAiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddScoped<AiRequestSigner>();
        services.AddHttpClient<IAiClient, AiClient>();
        services.AddHttpClient<IAiStreamingClient, AiStreamingClient>();
        return services;
    }
}

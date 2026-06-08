using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Modules.Secrets.Application.Abstractions;
using HarborAdmin.Modules.Secrets.Application.Services;
using HarborAdmin.Modules.Secrets.Infrastructure.Contexts;
using HarborAdmin.Modules.Secrets.Infrastructure.Repositories;
using HarborAdmin.Modules.Secrets.Infrastructure.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.Modules.Secrets;

/// <summary>
/// Secrets 模块依赖注入扩展。
/// </summary>
public static class SecretsModuleExtensions
{
    /// <summary>
    /// 注册 Secrets 管理模块。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">配置根。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddSecretsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISecretsDbContext, SecretsDbContext>();
        services.AddSingleton<ISecretsRepository, FreeSqlSecretsRepository>();
        services.TryAddScoped<SecretStore>();
        services.TryAddScoped<ISecretStore>(sp => sp.GetRequiredService<SecretStore>());
        services.TryAddScoped<ISecretResolver>(sp => sp.GetRequiredService<SecretStore>());
        services.AddScoped<SecretService>();
        return services;
    }
}

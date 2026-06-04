using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Application.Services;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Options;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.Modules.ConfigCenter;

/// <summary>
/// ConfigCenter 模块依赖注入扩展
/// </summary>
public static class ConfigCenterModuleExtensions
{
    /// <summary>
    /// 注册 ConfigCenter 模块服务（仓储、应用服务等）。
    /// 调用前须已执行 <c>AddHarborFreeSql</c>，实体扫描与库键映射由数据基础设施按模块约定完成。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置根</param>
    /// <returns>原服务集合</returns>
    public static IServiceCollection AddConfigCenterModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConfigCenterServerOptions>(configuration.GetSection(ConfigCenterServerOptions.SectionName));

        services.AddSingleton<IConfigCenterDbContext, ConfigCenterDbContext>();
        services.AddSingleton<IConfigCenterRepository, FreeSqlConfigCenterRepository>();
        services.TryAddSingleton<IConfigCenterNotifyClient, NoOpConfigCenterNotifyClient>();
        services.AddScoped<ConfigCenterService>();

        return services;
    }

}

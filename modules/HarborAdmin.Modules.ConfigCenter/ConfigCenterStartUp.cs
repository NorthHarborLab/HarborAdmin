using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Options;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.Modules.ConfigCenter;

/// <summary>
/// ConfigCenter 模块启动入口。
/// </summary>
public sealed class ConfigCenterStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    /// <inheritdoc />
    public override string ModuleName => "ConfigCenter";

    /// <inheritdoc />
    public override string GetDbKey() => "ConfigCenterDb";

    /// <summary>
    /// 注册 ConfigCenter 模块服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="context">模块注册上下文。</param>
    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        services.Configure<ConfigCenterServerOptions>(context.Configuration.GetSection(ConfigCenterServerOptions.SectionName));

        services.AddHarborModuleData<IConfigCenterDbContext, ConfigCenterDbContext, IConfigCenterRepository, FreeSqlConfigCenterRepository>(
            repositoryLifetime: ServiceLifetime.Scoped);
        services.AddScoped<IConfigApplicationRepository, ConfigApplicationRepository>();
        services.AddScoped<IConfigItemRepository, ConfigItemRepository>();
        services.TryAddSingleton<IConfigCenterNotifyClient, NoOpConfigCenterNotifyClient>();
        services.AddScoped<ConfigCenterSnapshotService>();
        services.AddScoped<ConfigSecretReferenceValidator>();
        services.AddScoped<ConfigCenterApplicationService>();
        services.AddScoped<ConfigCenterItemService>();
        services.AddScoped<ConfigCenterPublishService>();
    }
}

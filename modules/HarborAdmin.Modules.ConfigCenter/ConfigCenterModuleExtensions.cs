using FreeSql;
using HarborAdmin.Modules.ConfigCenter.Application;
using HarborAdmin.Modules.ConfigCenter.Contracts;
using HarborAdmin.Modules.ConfigCenter.Domain;
using HarborAdmin.Modules.ConfigCenter.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.ConfigCenter;

/// <summary>
/// ConfigCenter 模块依赖注入扩展
/// </summary>
public static class ConfigCenterModuleExtensions
{
    /// <summary>
    /// 注册 ConfigCenter 模块服务(FreeSql,仓储,应用服务,缓存等)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置根</param>
    /// <param name="registerNotifyClient">为 <see langword="true"/> 时注册 TCP 发布通知客户端(Host 使用);ConfigCenter 进程应为 <see langword="false"/></param>
    /// <returns>原服务集合,便于链式调用</returns>
    public static IServiceCollection AddConfigCenterModule(this IServiceCollection services,IConfiguration configuration, bool registerNotifyClient = false)
    {
        services.Configure<ConfigCenterServerOptions>(configuration.GetSection(ConfigCenterServerOptions.SectionName));
        services.Configure<ConfigCenterDatabaseOptions>(configuration.GetSection(ConfigCenterDatabaseOptions.SectionName));

        var dbOptions = configuration.GetSection(ConfigCenterDatabaseOptions.SectionName).Get<ConfigCenterDatabaseOptions>()
                        ?? new ConfigCenterDatabaseOptions();

        var freeSql = CreateFreeSql(dbOptions);
        freeSql.CodeFirst.SyncStructure(
            typeof(ConfigApplication),
            typeof(ConfigItem),
            typeof(ConfigRelease),
            typeof(ConfigReleaseItem));

        services.AddSingleton(freeSql);
        services.AddMemoryCache();
        services.AddSingleton<IConfigCenterRepository, FreeSqlConfigCenterRepository>();
        services.AddSingleton<ConfigCenterService>();
        services.AddSingleton<PublishedConfigCache>();

        if (registerNotifyClient)
        {
            services.AddSingleton<IConfigCenterNotifyClient, TcpConfigCenterNotifyClient>();
        }
        else
        {
            services.AddSingleton<IConfigCenterNotifyClient, NoOpConfigCenterNotifyClient>();
        }

        return services;
    }

    /// <summary>根据配置创建 FreeSql 实例</summary>
    /// <param name="options">数据库选项</param>
    /// <returns>已配置的 <see cref="IFreeSql"/></returns>
    /// <exception cref="NotSupportedException">不支持的 <see cref="ConfigCenterDatabaseOptions.DataType"/></exception>
    private static IFreeSql CreateFreeSql(ConfigCenterDatabaseOptions options)
    {
        var dataType = options.DataType.Trim().ToLowerInvariant() switch
        {
            "sqlite" => DataType.Sqlite,
            "postgresql" or "postgres" => DataType.PostgreSQL,
            "sqlserver" or "mssql" => DataType.SqlServer,
            _ => throw new NotSupportedException($"Database DataType '{options.DataType}' is not supported.")
        };

        return new FreeSqlBuilder()
            .UseConnectionString(dataType, options.ConnectionString)
            .UseAutoSyncStructure(false)
            .Build();
    }
}

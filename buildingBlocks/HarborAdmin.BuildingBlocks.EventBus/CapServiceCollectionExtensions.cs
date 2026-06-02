using System.Reflection;
using System.Text.Encodings.Web;
using DotNetCore.CAP;
using HarborAdmin.BuildingBlocks.EventBus.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// CAP 依赖注入扩展
/// </summary>
public static class CapServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Harbor CAP（RabbitMQ / InMemory + 可配置存储）
    /// </summary>
    public static CapBuilder AddHarborCap(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CapOptions>? configureCap = null)
    {
        var harborCap = configuration.GetSection(HarborCapOptions.SectionName).Get<HarborCapOptions>() ?? new HarborCapOptions();

        services.Configure<HarborCapOptions>(configuration.GetSection(HarborCapOptions.SectionName));
        services.AddSingleton<IEventPublisher, CapEventPublisher>();

        var builder = services.AddCap(options =>
        {
            options.DefaultGroupName = harborCap.DefaultGroup;
            options.Version = harborCap.Version;
            options.FailedRetryCount = harborCap.FailedRetryCount;
            options.FailedRetryInterval = harborCap.FailedRetryInterval;
            options.EnablePublishParallelSend = true;
            options.UseStorageLock = true;
            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            ConfigureStorage(options, harborCap.Storage);
            ConfigureTransport(options, harborCap);

            if (harborCap.UseDashboard)
            {
                options.UseDashboard();
            }

            configureCap?.Invoke(options);
        });

        return builder;
    }

    /// <summary>
    /// 注册 CAP 订阅程序集
    /// </summary>
    public static CapBuilder AddHarborCapSubscribers(this CapBuilder builder, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            builder.AddSubscriberAssembly(assembly);
        }

        return builder;
    }

    private static void ConfigureStorage(CapOptions options, CapStorageOptions storage)
    {
        switch (storage.Type.Trim().ToLowerInvariant())
        {
            case "inmemory":
                options.UseInMemoryStorage();
                break;
            case "postgres":
            case "postgresql":
                options.UsePostgreSql(storage.ConnectionString);
                break;
            case "sqlite":
            default:
                options.UseSqlite(storage.ConnectionString);
                break;
        }
    }

    private static void ConfigureTransport(CapOptions options, HarborCapOptions harborCap)
    {
        if (harborCap.Transport.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            options.UseInMemoryStorage();
            return;
        }

        var mq = harborCap.RabbitMq;
        options.UseRabbitMQ(mqConfig =>
        {
            mqConfig.HostName = mq.HostName;
            mqConfig.Port = mq.Port;
            mqConfig.UserName = mq.UserName;
            mqConfig.Password = mq.Password;
            mqConfig.ExchangeName = mq.ExchangeName;
        });
    }
}

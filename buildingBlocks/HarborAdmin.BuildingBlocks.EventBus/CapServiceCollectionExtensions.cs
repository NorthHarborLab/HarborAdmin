using System.Reflection;
using System.Text.Encodings.Web;
using DotNetCore.CAP;
using DotNetCore.Cap.RequestReply.Extensions;
using DotNetCore.Cap.RequestReply.Models;
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
    public static CapBuilder AddHarborCap(this IServiceCollection services, IConfiguration configuration, Action<CapOptions>? configureCap = null)
    {
        var harborCap = configuration.GetSection(HarborCapOptions.SectionName).Get<HarborCapOptions>() ?? new HarborCapOptions();

        services.Configure<HarborCapOptions>(configuration.GetSection(HarborCapOptions.SectionName));
        services.AddSingleton<IEventPublisher, CapEventPublisher>();
        if (harborCap.RequestReply.Enabled)
        {
            services.AddSingleton<IEventRequestClient, CapEventRequestClient>();
            services.AddCapRequestReply(options => ConfigureRequestReply(options, harborCap.RequestReply));
        }

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

    /// <summary>
    /// 配置 CAP 消息存储。
    /// </summary>
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

    /// <summary>
    /// 配置 CAP 消息传输。
    /// </summary>
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

    /// <summary>
    /// 配置 CAP Request/Reply 扩展。
    /// </summary>
    private static void ConfigureRequestReply(RequestReplyOptions options, CapRequestReplyOptions harborOptions)
    {
        options.ServiceName = harborOptions.ServiceName;
        options.InstanceId = harborOptions.InstanceId;
        options.DefaultTimeout = TimeSpan.FromSeconds(Math.Max(1, harborOptions.DefaultTimeoutSeconds));

        ConfigureRequestReplyTransport(options, harborOptions);
        ConfigureRequestReplyStore(options, harborOptions);

        if (harborOptions.EnableOpenTelemetryDiagnostics)
        {
            options.UseOpenTelemetryDiagnostics();
        }
    }

    /// <summary>
    /// 配置 Request/Reply 响应通道。
    /// </summary>
    private static void ConfigureRequestReplyTransport(RequestReplyOptions options, CapRequestReplyOptions harborOptions)
    {
        switch (harborOptions.Transport.Trim().ToLowerInvariant())
        {
            case "redis":
                options.UseRedisReply(redis =>
                {
                    redis.EndpointName = harborOptions.Redis.EndpointName;
                    redis.ConnectionString = harborOptions.Redis.ConnectionString;
                    redis.StreamPrefix = harborOptions.Redis.StreamPrefix;
                    foreach (var endpoint in harborOptions.Redis.Endpoints)
                    {
                        redis.AddEndpoint(endpoint.Key, endpoint.Value);
                    }
                });
                break;
            case "postgres":
            case "postgresql":
                options.UsePostgreSqlReply(postgreSql =>
                {
                    postgreSql.ConnectionString = harborOptions.PostgreSql.ConnectionString;
                    postgreSql.Schema = harborOptions.PostgreSql.Schema;
                    postgreSql.TableName = harborOptions.PostgreSql.ReplyTableName;
                    postgreSql.AutoCreateTable = harborOptions.PostgreSql.AutoCreateTable;
                });
                break;
            case "mysql":
                options.UseMySqlReply(mySql =>
                {
                    mySql.ConnectionString = harborOptions.MySql.ConnectionString;
                    mySql.TableNamePrefix = harborOptions.MySql.TableNamePrefix;
                    mySql.TableName = harborOptions.MySql.ReplyTableName;
                    mySql.AutoCreateTable = harborOptions.MySql.AutoCreateTable;
                });
                break;
            case "inmemory":
            default:
                options.UseInMemoryReply();
                break;
        }
    }

    /// <summary>
    /// 配置 Request/Reply 请求状态存储。
    /// </summary>
    private static void ConfigureRequestReplyStore(RequestReplyOptions options, CapRequestReplyOptions harborOptions)
    {
        switch (harborOptions.Store.Trim().ToLowerInvariant())
        {
            case "postgres":
            case "postgresql":
                options.UsePostgreSqlStore(postgreSql =>
                {
                    postgreSql.ConnectionString = harborOptions.PostgreSql.ConnectionString;
                    postgreSql.Schema = harborOptions.PostgreSql.Schema;
                    postgreSql.TableName = harborOptions.PostgreSql.StoreTableName;
                    postgreSql.AutoCreateTable = harborOptions.PostgreSql.AutoCreateTable;
                });
                break;
            case "mysql":
                options.UseMySqlStore(mySql =>
                {
                    mySql.ConnectionString = harborOptions.MySql.ConnectionString;
                    mySql.TableNamePrefix = harborOptions.MySql.TableNamePrefix;
                    mySql.TableName = harborOptions.MySql.StoreTableName;
                    mySql.AutoCreateTable = harborOptions.MySql.AutoCreateTable;
                });
                break;
            case "inmemory":
            default:
                options.UseInMemoryStore();
                break;
        }
    }
}
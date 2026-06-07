using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Infrastructure;
using HarborAdmin.BuildingBlocks.Caching.Internal;
using HarborAdmin.BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching;

/// <summary>
/// Harbor 缓存依赖注入扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Harbor 缓存基础设施。
    /// </summary>
    public static IServiceCollection AddHarborCaching(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.Configure<HarborCacheOptions>(configurationSection);
        services.AddMemoryCache();
        services.AddSingleton<CacheInvalidationRuleProvider>();
        services.AddSingleton<IHarborCache, HarborCache>();
        services.AddSingleton<HarborCacheInvalidator>();
        services.AddSingleton<IHarborCacheInvalidator>(sp => sp.GetRequiredService<HarborCacheInvalidator>());
        services.AddSingleton<IHarborEntityCacheInvalidator>(sp => sp.GetRequiredService<HarborCacheInvalidator>());
        services.AddSingleton<ICacheCatalogProvider, CacheCatalogProvider>();
        services.AddSingleton<IHarborCacheManager, HarborCacheManager>();

        var options = configurationSection.Get<HarborCacheOptions>() ?? new HarborCacheOptions();
        if (options.Provider is HarborCacheProvider.Redis or HarborCacheProvider.Garnet)
        {
            // Redis 和 Garnet 都兼容 Redis 协议，底层统一使用 StackExchange.Redis。
            var connectionString = options.Provider == HarborCacheProvider.Redis
                ? options.Redis.ConnectionString
                : options.Garnet.ConnectionString;

            services.AddStackExchangeRedisCache(redisOptions => { redisOptions.Configuration = connectionString; });

            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
            services.AddSingleton<IHarborRedisClient, HarborRedisClient>();
            // 分布式 Provider 必须使用 Redis Tag 索引，才能跨进程按 tag 失效。
            services.AddSingleton<ITagIndexStore, RedisTagIndexStore>();
            services.AddSingleton<IHarborRedisStructures, HarborRedisStructures>();
        }
        else
        {
            // Memory Provider 仍支持对象缓存和 tag 失效，但 Redis 原生结构入口会显式报错。
            services.AddSingleton<ITagIndexStore, MemoryTagIndexStore>();
            services.AddSingleton<IHarborRedisStructures, UnavailableRedisStructures>();
        }

        return services;
    }
}
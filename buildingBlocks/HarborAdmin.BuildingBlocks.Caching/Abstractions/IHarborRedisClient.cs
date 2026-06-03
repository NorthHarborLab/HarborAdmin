using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// Harbor Redis/Garnet 低层访问入口。
/// </summary>
public interface IHarborRedisClient
{
    /// <summary>
    /// Redis 连接复用器。
    /// </summary>
    IConnectionMultiplexer Connection { get; }

    /// <summary>
    /// 获取 Redis 数据库。
    /// </summary>
    IDatabase GetDatabase();

    /// <summary>
    /// 获取 Redis 订阅器。
    /// </summary>
    ISubscriber GetSubscriber();
}

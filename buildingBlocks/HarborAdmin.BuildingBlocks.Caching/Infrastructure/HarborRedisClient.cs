using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// StackExchange.Redis 连接访问实现。
/// </summary>
internal sealed class HarborRedisClient(IConnectionMultiplexer connection) : IHarborRedisClient
{
    public IConnectionMultiplexer Connection { get; } = connection;

    /// <summary>
    /// 获取默认 Redis 数据库。
    /// </summary>
    public IDatabase GetDatabase() => Connection.GetDatabase();

    /// <summary>
    /// 获取 Redis 发布订阅入口。
    /// </summary>
    public ISubscriber GetSubscriber() => Connection.GetSubscriber();
}
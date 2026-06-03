namespace HarborAdmin.BuildingBlocks.Caching.Options;

/// <summary>
/// Harbor 缓存配置。
/// </summary>
public sealed class HarborCacheOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Harbor:Cache";

    /// <summary>
    /// 缓存提供方。
    /// </summary>
    public HarborCacheProvider Provider { get; set; } = HarborCacheProvider.Memory;

    /// <summary>
    /// 全局 key 前缀。
    /// </summary>
    public string KeyPrefix { get; set; } = "harbor";

    /// <summary>
    /// 默认过期秒数。
    /// </summary>
    public int DefaultExpirationSeconds { get; set; } = 600;

    /// <summary>
    /// Redis 配置。
    /// </summary>
    public RedisCacheEndpointOptions Redis { get; set; } = new();

    /// <summary>
    /// Garnet 配置。
    /// </summary>
    public RedisCacheEndpointOptions Garnet { get; set; } = new();
}

/// <summary>
/// 缓存提供方。
/// </summary>
public enum HarborCacheProvider
{
    /// <summary>
    /// 仅使用进程内内存缓存。
    /// </summary>
    Memory = 0,

    /// <summary>
    /// 使用 Redis 作为二级缓存与 tag 索引存储。
    /// </summary>
    Redis = 1,

    /// <summary>
    /// 使用 Garnet 作为 Redis 协议兼容后端。
    /// </summary>
    Garnet = 2
}

/// <summary>
/// Redis 协议兼容缓存端点配置。
/// </summary>
public sealed class RedisCacheEndpointOptions
{
    /// <summary>
    /// 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";
}
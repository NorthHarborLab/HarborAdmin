using HarborAdmin.BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// 基于 Redis Set 的缓存 tag 索引存储。
/// Redis/Garnet Tag 索引。使用 Redis Set 保存 tag -> keys 与 key -> tags 的双向关系。
/// </summary>
internal sealed class RedisTagIndexStore(IConnectionMultiplexer connection, IOptions<HarborCacheOptions> options) : ITagIndexStore
{
    private readonly IDatabase _database = connection.GetDatabase();
    private readonly string _prefix = $"{options.Value.KeyPrefix}:cache";

    /// <summary>
    /// 绑定缓存 key 与 tag 集合。
    /// </summary>
    public async ValueTask BindAsync(string key, IReadOnlyCollection<string> tags, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        foreach (var tag in tags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // 建两份集合，既支持按 tag 找 key，也支持删除 key 时反查它绑定过哪些 tag。
            await _database.SetAddAsync(TagSetKey(tag), key);
            await _database.SetAddAsync(KeySetKey(key), tag);
            if (expiration.HasValue)
            {
                // 索引生命周期跟随缓存值，避免缓存过期后 tag 集合长期保存脏 key。
                await _database.KeyExpireAsync(TagSetKey(tag), expiration.Value);
                await _database.KeyExpireAsync(KeySetKey(key), expiration.Value);
            }
        }
    }

    /// <summary>
    /// 获取指定 tag 关联的缓存 key。
    /// </summary>
    public async ValueTask<IReadOnlyList<string>> GetKeysAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var values = await _database.SetMembersAsync(TagSetKey(tag));
        return values.Select(value => value.ToString()).ToArray();
    }

    /// <summary>
    /// 移除指定缓存 key 的索引关系。
    /// </summary>
    public async ValueTask RemoveKeyAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tags = await _database.SetMembersAsync(KeySetKey(key));
        foreach (var tag in tags)
        {
            // 从 tag -> keys 集合中移除当前 key，保持双向索引一致。
            await _database.SetRemoveAsync(TagSetKey(tag.ToString()), key);
        }

        await _database.KeyDeleteAsync(KeySetKey(key));
    }

    /// <summary>
    /// 移除指定 tag 的索引关系。
    /// </summary>
    public async ValueTask RemoveTagAsync(string tag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var keys = await _database.SetMembersAsync(TagSetKey(tag));
        foreach (var key in keys)
        {
            // tag 被整体删除时，也要从每个 key 的反向 tag 集合里移除该 tag。
            await _database.SetRemoveAsync(KeySetKey(key.ToString()), tag);
        }

        await _database.KeyDeleteAsync(TagSetKey(tag));
    }

    /// <summary>
    /// 构建 tag 到 key 集合的 Redis key。
    /// </summary>
    private RedisKey TagSetKey(string tag) => $"{_prefix}:tag:{tag}";

    /// <summary>
    /// 构建 key 到 tag 集合的 Redis key。
    /// </summary>
    private RedisKey KeySetKey(string key) => $"{_prefix}:key-tags:{key}";
}

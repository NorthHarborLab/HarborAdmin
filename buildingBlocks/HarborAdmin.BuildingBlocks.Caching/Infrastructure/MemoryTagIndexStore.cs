using System.Collections.Concurrent;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// 基于内存字典的缓存 tag 索引存储。
/// 进程内 Tag 索引。Memory Provider 使用它支持按 tag 反向删除缓存 key。
/// </summary>
internal sealed class MemoryTagIndexStore : ITagIndexStore
{
    // 双向索引：tag -> keys 用于按 tag 找 key；key -> tags 用于删除单 key 时回收索引。
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _keyTags = new(StringComparer.Ordinal);

    /// <summary>
    /// 绑定缓存 key 与 tag 集合。
    /// </summary>
    public ValueTask BindAsync(string key, IReadOnlyCollection<string> tags, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        if (tags.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        var keyTags = _keyTags.GetOrAdd(key, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        foreach (var tag in tags)
        {
            // ConcurrentDictionary 的 value 只作为 set 占位，实际只关心 key 是否存在。
            keyTags[tag] = 0;
            _tagKeys.GetOrAdd(tag, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))[key] = 0;
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 获取指定 tag 关联的缓存 key。
    /// </summary>
    public ValueTask<IReadOnlyList<string>> GetKeysAsync(string tag, CancellationToken cancellationToken)
    {
        if (!_tagKeys.TryGetValue(tag, out var keys))
        {
            return ValueTask.FromResult<IReadOnlyList<string>>([]);
        }

        return ValueTask.FromResult<IReadOnlyList<string>>(keys.Keys.ToArray());
    }

    /// <summary>
    /// 移除指定缓存 key 的索引关系。
    /// </summary>
    public ValueTask RemoveKeyAsync(string key, CancellationToken cancellationToken)
    {
        if (_keyTags.TryRemove(key, out var tags))
        {
            // 删除单 key 时必须从每个 tag 集合里移除，防止 tag 失效读到已删除 key。
            foreach (var tag in tags.Keys)
            {
                if (_tagKeys.TryGetValue(tag, out var keys))
                {
                    keys.TryRemove(key, out _);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 移除指定 tag 的索引关系。
    /// </summary>
    public ValueTask RemoveTagAsync(string tag, CancellationToken cancellationToken)
    {
        if (_tagKeys.TryRemove(tag, out var keys))
        {
            // 删除 tag 时同步剥离 key -> tag 反向关系，保持双向索引一致。
            foreach (var key in keys.Keys)
            {
                if (_keyTags.TryGetValue(key, out var tags))
                {
                    tags.TryRemove(tag, out _);
                }
            }
        }

        return ValueTask.CompletedTask;
    }
}
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Internal;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// Harbor 缓存失效服务实现。
/// 对外统一暴露手动失效和实体事件失效，内部都落到 key/tag 删除。
/// </summary>
internal sealed class HarborCacheInvalidator(IHarborCache cache, CacheInvalidationRuleProvider ruleProvider) : IHarborCacheInvalidator, IHarborEntityCacheInvalidator
{
    /// <summary>
    /// 按缓存 key 失效。
    /// </summary>
    public ValueTask InvalidateKeyAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    /// <summary>
    /// 按缓存 tag 失效。
    /// </summary>
    public ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default) =>
        cache.RemoveByTagAsync(tag, cancellationToken);

    /// <summary>
    /// 根据实体变更触发缓存 tag 失效。
    /// </summary>
    public async ValueTask InvalidateEntityAsync(object entity, string operation, CancellationToken cancellationToken = default)
    {
        var entityType = entity.GetType();
        var rules = ruleProvider.GetRules(entityType);
        foreach (var rule in rules)
        {
            // operation 当前只作为扩展点保留；匹配规则后按实体字段格式化 tag 并失效。
            var tag = TemplateFormatter.Format(rule.TagTemplate, entity);
            await cache.RemoveByTagAsync(tag, cancellationToken);
        }
    }
}
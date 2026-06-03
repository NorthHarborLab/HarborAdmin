using System.Reflection;
using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.BuildingBlocks.Caching.Internal;

/// <summary>
/// 缓存自动失效规则提供器。
/// 发现 [CacheTag] 规则，并按实体类型匹配数据库侧自动失效。
/// </summary>
internal sealed class CacheInvalidationRuleProvider
{
    // 规则只需要在进程内发现一次；Lazy 避免 DI 构建阶段就扫描所有程序集。
    private readonly Lazy<IReadOnlyList<CacheInvalidationRule>> _rules = new(DiscoverRules, true);

    /// <summary>
    /// 获取指定实体类型命中的缓存失效规则。
    /// </summary>
    public IReadOnlyList<CacheInvalidationRule> GetRules(Type entityType) =>
        _rules.Value
            // 允许规则声明在基类或接口上，实际实体类型仍然可以命中。
            .Where(rule => rule.EntityType.IsAssignableFrom(entityType))
            .ToArray();

    /// <summary>
    /// 扫描 HarborAdmin 程序集中的缓存失效规则。
    /// </summary>
    private static IReadOnlyList<CacheInvalidationRule> DiscoverRules()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            // 只扫描 HarborAdmin 程序集，降低反射成本并避免第三方程序集加载异常。
            .Where(IsHarborAssembly)
            .SelectMany(GetLoadableTypes)
            .SelectMany(GetRulesFromType)
            .ToArray();
    }

    /// <summary>
    /// 读取单个缓存模型类型声明的所有失效规则。
    /// </summary>
    private static IEnumerable<CacheInvalidationRule> GetRulesFromType(Type type)
    {
        foreach (var attribute in type.GetCustomAttributes(typeof(CacheTagAttribute), false)
                     .Cast<CacheTagAttribute>())
        {
            if (string.IsNullOrWhiteSpace(attribute.Template))
            {
                continue;
            }

            // CacheTag 同时可声明触发失效的实体，减少“绑定 tag”和“失效 tag”重复书写。
            foreach (var entityType in attribute.InvalidatesOn)
            {
                yield return new CacheInvalidationRule(entityType, attribute.Template);
            }
        }
    }

    /// <summary>
    /// 判断程序集是否属于 HarborAdmin 命名空间。
    /// </summary>
    private static bool IsHarborAssembly(Assembly assembly) =>
        assembly.GetName().Name?.StartsWith("HarborAdmin.", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 安全读取程序集中的可加载类型。
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // 某些类型加载失败时仍保留已成功加载的类型，避免一个坏类型阻断全部规则发现。
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }
}

/// <summary>
/// 实体类型与缓存 tag 模板之间的失效规则。
/// </summary>
internal sealed record CacheInvalidationRule(Type EntityType, string TagTemplate);

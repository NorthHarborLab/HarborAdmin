using System.Collections;
using System.Reflection;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;

namespace HarborAdmin.Host.Infrastructure;

internal static class CacheInvalidationAopBridge
{
    public static void Dispatch(IServiceProvider serviceProvider, object eventArgs)
    {
        IHarborEntityCacheInvalidator? invalidator;
        try
        {
            invalidator = serviceProvider.GetService<IHarborEntityCacheInvalidator>();
        }
        catch
        {
            // 缓存失效是旁路能力，缓存基础设施不可用时不能影响数据库读写主链路。
            return;
        }

        if (invalidator is null)
        {
            return;
        }

        var operation = ReadOperation(eventArgs);
        foreach (var entity in ReadEntities(eventArgs))
        {
            try
            {
                invalidator.InvalidateEntityAsync(entity, operation).AsTask().GetAwaiter().GetResult();
            }
            catch
            {
                // 缓存失效是旁路能力，不能让缓存基础设施故障回滚已经完成的数据库写入。
            }
        }
    }

    private static string ReadOperation(object eventArgs)
    {
        var eventType = eventArgs.GetType();
        foreach (var name in new[] { "CurdType", "CrudType", "Operation", "AopType" })
        {
            var property = eventType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            var value = property?.GetValue(eventArgs);
            if (value is not null)
            {
                return value.ToString() ?? eventType.Name;
            }
        }

        return eventType.Name;
    }

    private static IEnumerable<object> ReadEntities(object eventArgs)
    {
        var eventType = eventArgs.GetType();
        foreach (var name in new[] { "Object", "Entity", "Value" })
        {
            var property = eventType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property?.GetValue(eventArgs) is { } value)
            {
                foreach (var entity in Expand(value))
                {
                    yield return entity;
                }

                yield break;
            }
        }
    }

    private static IEnumerable<object> Expand(object value)
    {
        if (value is string)
        {
            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    yield return item;
                }
            }

            yield break;
        }

        yield return value;
    }
}

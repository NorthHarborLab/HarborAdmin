using System.Reflection;
using HarborAdmin.BuildingBlocks.Caching.Abstractions;
using HarborAdmin.BuildingBlocks.Caching.Attributes;
using HarborAdmin.BuildingBlocks.Caching.Internal;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// 缓存模型目录提供器。
/// </summary>
internal sealed class CacheCatalogProvider(CacheKeyNormalizer keyNormalizer) : ICacheCatalogProvider
{
    private readonly Lazy<IReadOnlyList<CacheModelDescriptor>> _models = new(() => DiscoverModels(keyNormalizer), true);

    /// <inheritdoc />
    public IReadOnlyList<CacheModelDescriptor> GetModels() => _models.Value;

    /// <inheritdoc />
    public IReadOnlyList<CacheGroupDescriptor> BuildGroups(IReadOnlyDictionary<string, int> activeTagCountsByPrefix)
    {
        return _models.Value
            .GroupBy(ResolveGroupPrefix)
            .Select(group =>
            {
                var models = group.OrderBy(model => model.Order).ThenBy(model => model.DisplayName, StringComparer.Ordinal).ToArray();
                var first = models[0];
                var groupPrefix = ResolveGroupPrefix(first);
                activeTagCountsByPrefix.TryGetValue(groupPrefix, out var activeTagCount);
                return new CacheGroupDescriptor(
                    groupPrefix,
                    ResolveGroupName(models),
                    first.Module,
                    models.Min(model => model.Order),
                    models,
                    activeTagCount);
            })
            .OrderBy(group => group.Order)
            .ThenBy(group => group.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc />
    public CacheModelDescriptor? MatchModelByKey(string key)
    {
        CacheModelDescriptor? best = null;
        var bestLength = -1;
        foreach (var model in _models.Value)
        {
            if (!IsKeyUnderPrefix(key, model.Prefix))
            {
                continue;
            }

            if (model.Prefix.Length > bestLength)
            {
                best = model;
                bestLength = model.Prefix.Length;
            }
        }

        return best;
    }

    private static string ResolveGroupPrefix(CacheModelDescriptor model) =>
        string.IsNullOrWhiteSpace(model.GroupPrefix) ? model.Prefix : model.GroupPrefix;

    private static string ResolveGroupName(IReadOnlyList<CacheModelDescriptor> models)
    {
        var explicitName = models.Select(model => model.GroupName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
        return string.IsNullOrWhiteSpace(explicitName) ? models[0].DisplayName : explicitName;
    }

    private static bool IsKeyUnderPrefix(string key, string prefix) =>
        string.Equals(key, prefix, StringComparison.Ordinal) ||
        key.StartsWith(prefix + ":", StringComparison.Ordinal);

    private static IReadOnlyList<CacheModelDescriptor> DiscoverModels(CacheKeyNormalizer keyNormalizer)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(IsHarborAssembly)
            .SelectMany(GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.GetCustomAttributes(typeof(CacheKeyAttribute), false).Length > 0)
            .Select(type => CreateDescriptor(type, keyNormalizer))
            .OrderBy(model => model.Order)
            .ThenBy(model => model.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    private static CacheModelDescriptor CreateDescriptor(Type modelType, CacheKeyNormalizer keyNormalizer)
    {
        var metadata = CacheModelMetadata.For(modelType);
        var catalog = modelType.GetCustomAttribute<CacheCatalogAttribute>();
        var tagTemplates = metadata.ClassTagTemplates
            .Concat(metadata.PropertyTagTemplates)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var supportsBulkClear = catalog is null
            ? tagTemplates.Length > 0
            : catalog.SupportsBulkClear && tagTemplates.Length > 0;
        var expirationSeconds = metadata.Expiration.HasValue ? (int?)metadata.Expiration.Value.TotalSeconds : null;

        return new CacheModelDescriptor(
            modelType.Name,
            catalog?.DisplayName ?? modelType.Name,
            catalog?.Module ?? string.Empty,
            catalog?.Order ?? 0,
            catalog?.Description ?? string.Empty,
            keyNormalizer.ApplyPrefix(metadata.Prefix),
            metadata.KeyTemplate,
            expirationSeconds,
            tagTemplates.Select(keyNormalizer.ApplyPrefix).ToArray(),
            catalog?.SensitiveFields ?? [],
            supportsBulkClear)
        {
            GroupPrefix = string.IsNullOrWhiteSpace(catalog?.GroupPrefix)
                ? keyNormalizer.ApplyPrefix(metadata.Prefix)
                : keyNormalizer.ApplyPrefix(catalog.GroupPrefix),
            GroupName = catalog?.GroupName ?? string.Empty
        };
    }

    private static bool IsHarborAssembly(Assembly assembly) =>
        assembly.GetName().Name?.StartsWith("HarborAdmin.", StringComparison.OrdinalIgnoreCase) == true;

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }
}

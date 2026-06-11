using System.Reflection;
using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data.Auth;
using HarborAdmin.BuildingBlocks.Data.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// FreeSql 依赖注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Harbor FreeSql 多库基础设施。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configurationSection">数据库配置节（<c>Harbor:DbConfig</c>）。</param>
    /// <param name="configure">可选注册选项，例如追加实体扫描程序集。</param>
    public static IServiceCollection AddHarborFreeSql(this IServiceCollection services, IConfigurationSection configurationSection,
        Action<HarborFreeSqlOptions>? configure = null)
    {
        var dbConfig = configurationSection.Get<DbConfig>() ?? new DbConfig();
        var databases = dbConfig.Databases;
        _ = databases.FirstOrDefault()
            ?? throw new InvalidOperationException("Harbor:DbConfig must contain at least one database.");
        var options = new HarborFreeSqlOptions();
        configure?.Invoke(options);
        ValidateDatabases(databases);
        var databaseMap = databases.ToDictionary(db => db.Key, StringComparer.OrdinalIgnoreCase);

        // 扫描模块元数据和实体并建立数据库映射，后续仓储和 UoW 都依赖这份注册表定位数据库。
        var moduleDescriptors = DiscoverModules(options).ToArray();
        var moduleMappings = DiscoverModuleMappings(moduleDescriptors, databases);
        var entityMappings = DiscoverEntityMappings(moduleDescriptors, moduleMappings, databases);
        var entityRegistry = DbEntityRegistry.Create(entityMappings);
        var moduleRegistry = DbModuleRegistry.Create(moduleMappings);
        var defaultDbKey = databases[0].Key;

        services.TryAddSingleton<ICurrentUser, NullCurrentUser>();
        services.AddSingleton(entityRegistry);
        services.AddSingleton(moduleRegistry);
        services.AddSingleton<HarborFreeSqlCloud>(sp =>
        {
            var currentUser = sp.GetService<ICurrentUser>();
            var cloud = new HarborFreeSqlCloud();

            foreach (var db in databases)
            {
                // 每个 Harbor:DbConfig 条目注册为一个 FreeSqlCloud 实例，真正使用时通过 cloud.Use(dbKey) 取出。
                DbRegistration.RegisterDb(cloud, db, currentUser, options.SnowflakeWorkerId, sp, options.CurdAfterHandlers);
            }

            foreach (var group in entityMappings.GroupBy(mapping => mapping.DbKey, StringComparer.OrdinalIgnoreCase))
            {
                var db = databaseMap[group.Key];
                var entityTypes = group.Select(mapping => mapping.EntityType).ToArray();
                if (entityTypes.Length > 0 && db is { SyncStructure: true, ReadOnly: false })
                {
                    // 只对可写库执行 CodeFirst 同步，避免只读库或从库被结构变更污染。
                    var fsql = cloud.Use(db.Key);
                    foreach (var hook in sp.GetServices<IHarborFreeSqlPreSyncHook>())
                    {
                        hook.BeforeSyncStructure(fsql, db.Key, entityTypes);
                    }

                    DbRegistration.SyncStructure(fsql, entityTypes);
                }
            }

            return cloud;
        });

        services.AddSingleton<IFreeSql>(sp => sp.GetRequiredService<HarborFreeSqlCloud>().Use(defaultDbKey));
        services.AddScoped<UnitOfWorkManagerCloud>();
        services.AddHostedService<HarborFreeSqlInitializerHostedService>();

        return services;
    }

    /// <summary>
    /// 校验数据库配置，避免缺失字段或重复 key 导致 FreeSqlCloud 注册冲突。
    /// </summary>
    private static void ValidateDatabases(IReadOnlyList<DbConnectionConfig> databases)
    {
        var configuredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var db in databases)
        {
            if (string.IsNullOrWhiteSpace(db.Key))
            {
                throw new InvalidOperationException("Harbor:DbConfig:Databases contains a database with empty Key.");
            }

            if (string.IsNullOrWhiteSpace(db.DataType))
            {
                throw new InvalidOperationException($"Harbor:DbConfig:Databases:{db.Key} must define DataType.");
            }

            if (string.IsNullOrWhiteSpace(db.ConnectionString))
            {
                throw new InvalidOperationException($"Harbor:DbConfig:Databases:{db.Key} must define ConnectionString.");
            }

            if (!configuredKeys.Add(db.Key))
            {
                throw new InvalidOperationException($"Harbor:DbConfig:Databases contains duplicate database Key '{db.Key}'.");
            }
        }
    }

    /// <summary>
    /// 发现模块与数据库 Key 的映射。
    /// </summary>
    private static IReadOnlyList<ModuleDbMapping> DiscoverModuleMappings(IReadOnlyList<ModuleDescriptor> modules, IReadOnlyList<DbConnectionConfig> databases)
    {
        if (databases.Count == 1)
        {
            // 单库模式没有归属歧义，所有模块统一映射到唯一数据库，兼容 ConfigCenter/Worker 等单库进程。
            return modules
                .Select(module => new ModuleDbMapping(module.MetadataType, databases[0].Key))
                .ToArray();
        }

        var dbKeys = databases.Select(db => db.Key).ToArray();
        return modules
            .Select(module => new ModuleDbMapping(module.MetadataType, GetRequiredDbKey(module, dbKeys)))
            .ToArray();
    }

    /// <summary>
    /// 发现实体并读取每个实体所属的数据库 Key。
    /// </summary>
    private static IReadOnlyList<EntityDbMapping> DiscoverEntityMappings(
        IReadOnlyList<ModuleDescriptor> modules,
        IReadOnlyList<ModuleDbMapping> moduleMappings,
        IReadOnlyList<DbConnectionConfig> databases)
    {
        var moduleDbKeys = moduleMappings.ToDictionary(mapping => mapping.MetadataType, mapping => mapping.DbKey);
        if (databases.Count == 1)
        {
            // 单库模式没有归属歧义，实体级覆盖不参与映射，避免单库进程加载多模块时被覆盖声明卡住。
            return modules
                .SelectMany(module => module.EntityTypes.Select(entityType => new EntityDbMapping(entityType, moduleDbKeys[module.MetadataType])))
                .ToArray();
        }

        var dbKeys = databases.Select(db => db.Key).ToArray();
        return modules
            .SelectMany(module => module.EntityTypes.Select(entityType =>
                new EntityDbMapping(entityType, ResolveEntityDbKey(entityType, moduleDbKeys[module.MetadataType], dbKeys))))
            .ToArray();
    }

    /// <summary>
    /// 发现模块元数据与模块实体。
    /// </summary>
    private static IEnumerable<ModuleDescriptor> DiscoverModules(HarborFreeSqlOptions options)
    {
        foreach (var assembly in HarborModuleAssemblyDiscovery.Discover(options.ModuleAssemblies))
        {
            var types = GetLoadableTypes(assembly);
            var startupTypes = types
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IHarborModuleStartup).IsAssignableFrom(type))
                .ToArray();

            if (startupTypes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Module assembly '{assembly.GetName().Name}' must declare exactly one IHarborModuleStartup implementation.");
            }

            if (startupTypes.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Module assembly '{assembly.GetName().Name}' declares multiple IHarborModuleStartup implementations: {string.Join(", ", startupTypes.Select(type => type.FullName))}.");
            }

            var metadata = Activator.CreateInstance(startupTypes[0]) as IHarborModuleStartup
                           ?? throw new InvalidOperationException(
                               $"Module startup '{startupTypes[0].FullName}' must provide a public parameterless constructor.");
            var entityTypes = types
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(EntityBase).IsAssignableFrom(type))
                .Distinct()
                .ToArray();

            yield return new ModuleDescriptor(assembly, startupTypes[0], metadata, entityTypes);
        }
    }

    /// <summary>
    /// 安全获取程序集类型列表；当部分类型加载失败时，仍返回已成功加载的类型。
    /// </summary>
    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
    }

    /// <summary>
    /// 读取模块元数据上的数据库 Key，并校验配置中是否存在对应数据库。
    /// </summary>
    private static string GetRequiredDbKey(ModuleDescriptor module, IReadOnlyList<string> dbKeys)
    {
        var declaredDbKey = module.Metadata.GetDbKey();
        if (string.IsNullOrWhiteSpace(declaredDbKey))
        {
            throw new InvalidOperationException(
            $"Module startup '{module.MetadataType.FullName}' must return a non-empty database key.");
        }

        // 保留配置文件中的原始大小写，避免后续 cloud.Use(dbKey) 与注册 key 表示不一致。
        var dbKey = dbKeys.FirstOrDefault(key => string.Equals(key, declaredDbKey, StringComparison.OrdinalIgnoreCase));
        if (dbKey is not null)
        {
            return dbKey;
        }

        throw new InvalidOperationException(
            $"Module startup '{module.MetadataType.FullName}' declares database key '{declaredDbKey}', but Harbor:DbConfig:Databases does not contain it.");
    }

    /// <summary>
    /// 解析实体最终数据库 Key；实体覆盖优先，未覆盖时使用模块默认 Key。
    /// </summary>
    private static string ResolveEntityDbKey(Type entityType, string moduleDbKey, IReadOnlyList<string> dbKeys)
    {
        var overrideAttribute = entityType.GetCustomAttribute<OverrideDbKeyAttribute>();
        if (overrideAttribute is null)
        {
            return moduleDbKey;
        }

        if (string.IsNullOrWhiteSpace(overrideAttribute.Key))
        {
            throw new InvalidOperationException(
                $"Entity '{entityType.FullName}' declares [OverrideDbKey] with an empty database key.");
        }

        // 保留配置文件中的原始大小写，避免后续 cloud.Use(dbKey) 与注册 key 表示不一致。
        var dbKey = dbKeys.FirstOrDefault(key => string.Equals(key, overrideAttribute.Key, StringComparison.OrdinalIgnoreCase));
        if (dbKey is not null)
        {
            return dbKey;
        }

        throw new InvalidOperationException(
            $"Entity '{entityType.FullName}' declares override database key '{overrideAttribute.Key}', but Harbor:DbConfig:Databases does not contain it.");
    }

    private sealed record ModuleDescriptor(Assembly Assembly, Type MetadataType, IHarborModuleMetadata Metadata, IReadOnlyList<Type> EntityTypes);
}

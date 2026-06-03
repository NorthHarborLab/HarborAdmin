using System.Reflection;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
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
    /// <param name="configurationSection">数据库配置节（<c>DbConfig</c>）。</param>
    /// <param name="configure">可选注册选项，例如追加实体扫描程序集。</param>
    public static IServiceCollection AddHarborFreeSql(this IServiceCollection services, IConfigurationSection configurationSection,
        Action<HarborFreeSqlOptions>? configure = null)
    {
        var dbConfig = configurationSection.Get<DbConfig>() ?? new DbConfig();
        var databases = dbConfig.Databases;
        _ = databases.FirstOrDefault()
            ?? throw new InvalidOperationException("DbConfig must contain at least one database.");
        var options = new HarborFreeSqlOptions();
        configure?.Invoke(options);
        ValidateDatabases(databases);
        var databaseMap = databases.ToDictionary(db => db.Key, StringComparer.OrdinalIgnoreCase);

        // 扫描模块实体并建立“实体类型 -> 数据库 Key”映射，后续仓储和 UoW 都依赖这份注册表定位数据库。
        var entityMappings = DiscoverEntityMappings(options, databases);
        var entityRegistry = DbEntityRegistry.Create(entityMappings);
        var defaultDbKey = databases[0].Key;

        services.TryAddSingleton<ICurrentUser, NullCurrentUser>();
        services.AddSingleton(entityRegistry);
        services.AddSingleton<HarborFreeSqlCloud>(sp =>
        {
            var currentUser = sp.GetService<ICurrentUser>();
            var cloud = new HarborFreeSqlCloud();

            foreach (var db in databases)
            {
                // 每个 DbConfig 条目注册为一个 FreeSqlCloud 实例，真正使用时通过 cloud.Use(dbKey) 取出。
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
                throw new InvalidOperationException("DbConfig:Databases contains a database with empty Key.");
            }

            if (string.IsNullOrWhiteSpace(db.DataType))
            {
                throw new InvalidOperationException($"DbConfig:Databases:{db.Key} must define DataType.");
            }

            if (string.IsNullOrWhiteSpace(db.ConnectionString))
            {
                throw new InvalidOperationException($"DbConfig:Databases:{db.Key} must define ConnectionString.");
            }

            if (!configuredKeys.Add(db.Key))
            {
                throw new InvalidOperationException($"DbConfig:Databases contains duplicate database Key '{db.Key}'.");
            }
        }
    }

    /// <summary>
    /// 发现实体并读取每个实体所属的数据库 Key。
    /// </summary>
    private static IReadOnlyList<EntityDbMapping> DiscoverEntityMappings(HarborFreeSqlOptions options, IReadOnlyList<DbConnectionConfig> databases)
    {
        var entityTypes = DiscoverEntityTypes(options).ToArray();
        if (entityTypes.Length == 0)
        {
            return [];
        }

        if (databases.Count == 1)
        {
            // 单库模式没有归属歧义，所有模块实体统一映射到唯一数据库。
            return entityTypes
                .Select(entityType => new EntityDbMapping(entityType, databases[0].Key))
                .ToArray();
        }

        var dbKeys = databases.Select(db => db.Key).ToArray();
        return entityTypes
            // 多库模式必须在实体上显式声明 [DbKey]，避免模块名推导规则对新人不透明。
            .Select(entityType => new EntityDbMapping(entityType, GetRequiredDbKey(entityType, dbKeys)))
            .ToArray();
    }

    /// <summary>
    /// 从候选程序集里筛选所有继承 <see cref="EntityBase"/> 的实体类型。
    /// </summary>
    private static IEnumerable<Type> DiscoverEntityTypes(HarborFreeSqlOptions options)
    {
        return DiscoverEntityAssemblies(options)
            .SelectMany(GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(EntityBase).IsAssignableFrom(type))
            .Distinct();
    }

    /// <summary>
    /// 获取需要扫描实体的程序集：用户显式追加的程序集优先，再补充入口程序集引用的 HarborAdmin 模块程序集。
    /// </summary>
    private static IEnumerable<Assembly> DiscoverEntityAssemblies(HarborFreeSqlOptions options)
    {
        foreach (var assembly in options.EntityAssemblies)
        {
            yield return assembly;
        }

        var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(IsModuleAssembly))
        {
            loadedNames.Add(assembly.GetName().Name!);
            yield return assembly;
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
        {
            yield break;
        }

        foreach (var reference in entryAssembly.GetReferencedAssemblies().Where(IsModuleAssemblyName))
        {
            if (!loadedNames.Add(reference.Name!))
            {
                continue;
            }

            // 引用程序集可能尚未被 CLR 加载，这里主动加载以便后续反射扫描实体。
            yield return Assembly.Load(reference);
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
    /// 读取实体上的 <see cref="DbKeyAttribute"/>，并校验配置中是否存在对应数据库。
    /// </summary>
    private static string GetRequiredDbKey(Type entityType, IReadOnlyList<string> dbKeys)
    {
        var attribute = entityType.GetCustomAttribute<DbKeyAttribute>();
        if (attribute is null || string.IsNullOrWhiteSpace(attribute.Key))
        {
            throw new InvalidOperationException(
                $"Entity '{entityType.FullName}' must declare [DbKey(\"...\")] when DbConfig contains multiple databases.");
        }

        // 保留配置文件中的原始大小写，避免后续 cloud.Use(dbKey) 与注册 key 表示不一致。
        var dbKey = dbKeys.FirstOrDefault(key => string.Equals(key, attribute.Key, StringComparison.OrdinalIgnoreCase));
        if (dbKey is not null)
        {
            return dbKey;
        }

        throw new InvalidOperationException(
            $"Entity '{entityType.FullName}' declares database key '{attribute.Key}', but DbConfig:Databases does not contain it.");
    }

    /// <summary>
    /// 判断已加载程序集是否为 HarborAdmin 业务模块程序集。
    /// </summary>
    private static bool IsModuleAssembly(Assembly assembly) =>
        IsModuleAssemblyName(assembly.GetName());

    /// <summary>
    /// 判断程序集名称是否符合 HarborAdmin 业务模块命名约定。
    /// </summary>
    private static bool IsModuleAssemblyName(AssemblyName assemblyName) =>
        assemblyName.Name?.StartsWith("HarborAdmin.Modules.", StringComparison.OrdinalIgnoreCase) == true;
}

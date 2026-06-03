using System.Reflection;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// <see cref="ServiceCollectionExtensions.AddHarborFreeSql"/> 注册选项
/// </summary>
public sealed class HarborFreeSqlOptions
{
    private readonly List<Assembly> _entityAssemblies = [];
    private readonly List<Action<IServiceProvider, object>> _curdAfterHandlers = [];

    /// <summary>
    /// Yitter 雪花 ID 的 WorkerId；不同 Host 实例应配置不同值。
    /// </summary>
    public ushort SnowflakeWorkerId { get; set; } = 1;

    /// <summary>
    /// 追加需要扫描实体的程序集。默认会扫描当前应用引用的 <c>HarborAdmin.Modules.*</c> 程序集。
    /// </summary>
    public HarborFreeSqlOptions AddEntityAssembly(Assembly assembly)
    {
        _entityAssemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// 追加 FreeSql 写入完成后的旁路处理器。
    /// </summary>
    /// <remarks>
    /// 处理器不应影响数据库写入结果；需要保证自身异常隔离。
    /// </remarks>
    public HarborFreeSqlOptions AddCurdAfterHandler(Action<IServiceProvider, object> handler)
    {
        _curdAfterHandlers.Add(handler);
        return this;
    }

    internal IReadOnlyList<Assembly> EntityAssemblies => _entityAssemblies;

    internal IReadOnlyList<Action<IServiceProvider, object>> CurdAfterHandlers => _curdAfterHandlers;
}

/// <summary>
/// 实体类型到数据库键的注册表。
/// </summary>
public sealed class DbEntityRegistry
{
    private readonly IReadOnlyDictionary<Type, string> _entityDbKeys;

    private DbEntityRegistry(IReadOnlyDictionary<Type, string> entityDbKeys)
    {
        _entityDbKeys = entityDbKeys;
    }

    /// <summary>
    /// 根据实体映射创建注册表。
    /// </summary>
    internal static DbEntityRegistry Create(IEnumerable<EntityDbMapping> mappings)
    {
        var map = new Dictionary<Type, string>();
        foreach (var mapping in mappings)
        {
            if (map.TryGetValue(mapping.EntityType, out var existingDbKey))
            {
                throw new InvalidOperationException(
                    $"Entity '{mapping.EntityType.FullName}' is already mapped to database '{existingDbKey}' and cannot be mapped to '{mapping.DbKey}'.");
            }

            map[mapping.EntityType] = mapping.DbKey;
        }

        return new DbEntityRegistry(map);
    }

    /// <summary>
    /// 获取实体类型对应的数据库键。
    /// </summary>
    public string GetDbKey<TEntity>() => GetDbKey(typeof(TEntity));

    /// <summary>
    /// 获取实体类型对应的数据库键。
    /// </summary>
    public string GetDbKey(Type entityType) =>
        _entityDbKeys.TryGetValue(entityType, out var dbKey)
            ? dbKey
            : throw new KeyNotFoundException($"Entity '{entityType.FullName}' is not mapped to any database.");

    /// <summary>
    /// 尝试获取实体类型对应的数据库键。
    /// </summary>
    public bool TryGetDbKey(Type entityType, out string dbKey) =>
        _entityDbKeys.TryGetValue(entityType, out dbKey!);
}

internal sealed record EntityDbMapping(Type EntityType, string DbKey);
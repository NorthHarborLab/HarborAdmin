using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data.Configs;

namespace HarborAdmin.BuildingBlocks.Data.DbContext;

/// <summary>
/// Harbor 模块数据库上下文基类。
/// </summary>
/// <typeparam name="TMetadata">模块元数据类型。</typeparam>
public abstract class HarborModuleDbContext<TMetadata> : IHarborModuleDbContext
    where TMetadata : IHarborModuleMetadata
{
    private readonly AsyncLocal<Dictionary<string, IFreeSql>?> _overrides = new();
    private readonly HarborFreeSqlCloud _cloud;

    /// <summary>
    /// 初始化模块数据库上下文。
    /// </summary>
    protected HarborModuleDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    {
        _cloud = cloud;
        DbKey = moduleRegistry.GetDbKey<TMetadata>();
    }

    /// <inheritdoc />
    public string DbKey { get; }

    /// <inheritdoc />
    public IFreeSql Orm => GetOrm(DbKey);

    /// <inheritdoc />
    public IFreeSql GetOrm(string dbKey)
    {
        if (string.IsNullOrWhiteSpace(dbKey))
        {
            throw new ArgumentException("Database key is required.", nameof(dbKey));
        }

        return _overrides.Value is not null && _overrides.Value.TryGetValue(dbKey, out var orm)
            ? orm
            : _cloud.Use(dbKey);
    }

    /// <inheritdoc />
    public IDisposable Bind(IFreeSql orm) => Bind(DbKey, orm);

    /// <inheritdoc />
    public IDisposable Bind(string dbKey, IFreeSql orm)
    {
        if (string.IsNullOrWhiteSpace(dbKey))
        {
            throw new ArgumentException("Database key is required.", nameof(dbKey));
        }

        var current = _overrides.Value;
        IFreeSql? previous = null;
        var hadPrevious = false;
        if (current is not null)
        {
            hadPrevious = current.TryGetValue(dbKey, out previous);
        }

        var next = current is null
            ? new Dictionary<string, IFreeSql>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, IFreeSql>(current, StringComparer.OrdinalIgnoreCase);
        next[dbKey] = orm;
        _overrides.Value = next;
        return new BindScope(this, dbKey, hadPrevious, previous);
    }

    /// <summary>
    /// 临时绑定工作单元 ORM 的释放作用域。
    /// </summary>
    private sealed class BindScope(HarborModuleDbContext<TMetadata> context, string dbKey, bool hadPrevious, IFreeSql? previous) : IDisposable
    {
        /// <summary>
        /// 释放绑定并恢复前一个 ORM。
        /// </summary>
        public void Dispose()
        {
            var current = context._overrides.Value;
            if (current is null)
            {
                return;
            }

            var next = new Dictionary<string, IFreeSql>(current, StringComparer.OrdinalIgnoreCase);
            if (hadPrevious && previous is not null)
            {
                next[dbKey] = previous;
            }
            else
            {
                next.Remove(dbKey);
            }

            context._overrides.Value = next.Count == 0 ? null : next;
        }
    }
}
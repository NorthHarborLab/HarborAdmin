using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

/// <inheritdoc cref="IConfigCenterDbContext"/>
public sealed class ConfigCenterDbContext(HarborFreeSqlCloud cloud, DbEntityRegistry entityRegistry) : IConfigCenterDbContext
{
    private readonly AsyncLocal<IFreeSql?> _override = new();

    /// <inheritdoc />
    public IFreeSql Orm => _override.Value ?? cloud.Use(entityRegistry.GetDbKey<ConfigApplication>());

    /// <inheritdoc />
    public IDisposable Bind(IFreeSql orm)
    {
        _override.Value = orm;
        return new BindScope(this);
    }

    /// <summary>
    /// 临时绑定 FreeSql 实例的释放作用域。
    /// </summary>
    private sealed class BindScope(ConfigCenterDbContext context) : IDisposable
    {
        /// <summary>
        /// 释放临时绑定并恢复默认 FreeSql 实例解析。
        /// </summary>
        public void Dispose() => context._override.Value = null;
    }
}

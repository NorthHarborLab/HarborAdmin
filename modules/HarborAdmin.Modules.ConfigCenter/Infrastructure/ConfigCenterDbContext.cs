using FreeSql;
using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure;

/// <inheritdoc cref="IConfigCenterDbContext"/>
public sealed class ConfigCenterDbContext(
    HarborFreeSqlCloud cloud,
    DbEntityRegistry entityRegistry) : IConfigCenterDbContext
{
    private readonly AsyncLocal<IFreeSql?> _override = new();

    /// <inheritdoc />
    public IFreeSql Orm => _override.Value ?? cloud.Use(entityRegistry.GetDbKey<Domain.ConfigApplication>());

    /// <inheritdoc />
    public IDisposable Bind(IFreeSql orm)
    {
        _override.Value = orm;
        return new BindScope(this);
    }

    private sealed class BindScope(ConfigCenterDbContext context) : IDisposable
    {
        public void Dispose() => context._override.Value = null;
    }
}

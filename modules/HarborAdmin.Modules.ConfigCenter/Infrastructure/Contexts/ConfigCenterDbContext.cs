using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Contexts;

/// <inheritdoc cref="IConfigCenterDbContext"/>
public sealed class ConfigCenterDbContext(
    HarborFreeSqlCloud cloud,
    DbEntityRegistry entityRegistry) : IConfigCenterDbContext
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

    private sealed class BindScope(ConfigCenterDbContext context) : IDisposable
    {
        public void Dispose() => context._override.Value = null;
    }
}

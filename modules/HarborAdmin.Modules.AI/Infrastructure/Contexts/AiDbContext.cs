using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Contexts;

/// <inheritdoc cref="IAiDbContext"/>
public sealed class AiDbContext(HarborFreeSqlCloud cloud, DbEntityRegistry entityRegistry) : IAiDbContext
{
    private readonly AsyncLocal<IFreeSql?> _override = new();

    /// <inheritdoc />
    public IFreeSql Orm => _override.Value ?? cloud.Use(entityRegistry.GetDbKey<AiProvider>());

    /// <inheritdoc />
    public IDisposable Bind(IFreeSql orm)
    {
        _override.Value = orm;
        return new BindScope(this);
    }

    private sealed class BindScope(AiDbContext context) : IDisposable
    {
        public void Dispose() => context._override.Value = null;
    }
}


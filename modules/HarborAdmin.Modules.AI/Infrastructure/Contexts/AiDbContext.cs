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

    /// <summary>
    /// 临时绑定工作单元 ORM 的释放作用域。
    /// </summary>
    private sealed class BindScope(AiDbContext context) : IDisposable
    {
        /// <summary>
        /// 释放绑定并恢复默认 FreeSqlCloud 解析。
        /// </summary>
        public void Dispose() => context._override.Value = null;
    }
}


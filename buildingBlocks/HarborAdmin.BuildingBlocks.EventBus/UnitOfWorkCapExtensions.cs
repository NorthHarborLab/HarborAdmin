using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// FreeSql 工作单元与 CAP 事务桥接
/// </summary>
public sealed class FreeSqlCapTransaction : CapTransactionBase
{
    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="dispatcher">CAP 调度器</param>
    /// <param name="uow">FreeSql 工作单元</param>
    public FreeSqlCapTransaction(IDispatcher dispatcher, IUnitOfWork uow) : base(dispatcher)
    {
        Uow = uow;
    }

    /// <summary>
    /// FreeSql 工作单元
    /// </summary>
    public IUnitOfWork Uow { get; }

    /// <inheritdoc />
    public override object? DbTransaction => Uow.GetOrBeginTransaction();

    /// <inheritdoc />
    public override void Commit()
    {
        Uow.Commit();
        Flush();
    }

    /// <inheritdoc />
    public override Task CommitAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use synchronous Commit with FreeSql CAP transaction.");

    /// <inheritdoc />
    public override void Rollback() => Uow.Rollback();

    /// <inheritdoc />
    public override Task RollbackAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use synchronous Rollback with FreeSql CAP transaction.");

    /// <inheritdoc />
    public override void Dispose() => Uow.Dispose();
}

/// <summary>
/// CAP 事务扩展
/// </summary>
public static class UnitOfWorkCapExtensions
{
    /// <summary>
    /// 在工作单元上开启 CAP 分布式事务
    /// </summary>
    public static ICapTransaction BeginCapTran(this IUnitOfWork unitOfWork, ICapPublisher capPublisher, bool autoCommit = false)
    {
        var dispatcher = capPublisher.ServiceProvider.GetRequiredService<IDispatcher>();
        var transaction = new FreeSqlCapTransaction(dispatcher, unitOfWork)
        {
            AutoCommit = autoCommit
        };
        return capPublisher.Transaction = transaction;
    }
}

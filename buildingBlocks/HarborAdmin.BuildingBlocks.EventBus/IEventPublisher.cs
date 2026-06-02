namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// 集成事件发布抽象接口
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 发布集成事件（参与当前 CAP 事务时需先 <see cref="UnitOfWorkCapExtensions.BeginCapTran"/>）
    /// </summary>
    Task PublishAsync<T>(string name, T payload, CancellationToken cancellationToken = default)
        where T : class;
}

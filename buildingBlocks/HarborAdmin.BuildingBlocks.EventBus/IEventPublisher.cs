namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// 集成事件发布抽象接口
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 发布集成事件。
    /// </summary>
    Task PublishAsync<T>(string name, T payload, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发布强类型集成事件。
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class, IIntegrationEvent;
}
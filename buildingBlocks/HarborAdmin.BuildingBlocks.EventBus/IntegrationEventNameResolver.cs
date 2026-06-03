using System.Reflection;

namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// Harbor 集成事件名称解析器。
/// </summary>
public static class IntegrationEventNameResolver
{
    /// <summary>
    /// 解析指定集成事件类型的 CAP topic 名称。
    /// </summary>
    public static string Resolve(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new InvalidOperationException($"Type '{eventType.FullName}' must implement {nameof(IIntegrationEvent)}.");
        }

        var attribute = eventType.GetCustomAttribute<EventNameAttribute>(false);
        if (attribute is null || string.IsNullOrWhiteSpace(attribute.Name))
        {
            throw new InvalidOperationException($"Integration event '{eventType.FullName}' must declare [EventName(\"...\")].");
        }

        return attribute.Name;
    }

    /// <summary>
    /// 解析指定集成事件类型的 CAP topic 名称。
    /// </summary>
    public static string Resolve<TEvent>() where TEvent : IIntegrationEvent => Resolve(typeof(TEvent));

    /// <summary>
    /// 解析指定集成事件实例的 CAP topic 名称。
    /// </summary>
    public static string Resolve(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return Resolve(@event.GetType());
    }
}
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// 基于 <see cref="ICapPublisher"/> 的事件发布实现。
/// </summary>
public sealed class CapEventPublisher(ICapPublisher capPublisher, ILogger<CapEventPublisher> logger) : IEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync<T>(string name, T payload, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        logger.LogInformation(
            "Publishing CAP event {EventName} with payload type {PayloadType}.",
            name,
            typeof(T).FullName);

        return capPublisher.PublishAsync(name, payload, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventName = IntegrationEventNameResolver.Resolve(@event);
        logger.LogInformation(
            "Publishing integration event {EventName} {EventId} {CorrelationId} {EventType}.",
            eventName,
            @event.Id,
            @event.CorrelationId,
            @event.GetType().FullName);

        return capPublisher.PublishAsync(eventName, @event, cancellationToken: cancellationToken);
    }
}
namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// Harbor 集成事件基类。
/// </summary>
public abstract class IntegrationEventBase : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    public string? TraceId { get; init; }

    /// <inheritdoc />
    public string Version { get; init; } = "v1";
}

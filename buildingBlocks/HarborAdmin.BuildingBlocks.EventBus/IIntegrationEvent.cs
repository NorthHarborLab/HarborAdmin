namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// Harbor 集成事件契约。
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件唯一标识。
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// 事件发生时间。
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// 业务调用链关联标识。
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// 分布式追踪标识。
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// 事件契约版本。
    /// </summary>
    string Version { get; }
}

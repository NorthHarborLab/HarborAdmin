using DotNetCore.CAP;

namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// 基于 <see cref="ICapPublisher"/> 的事件发布实现
/// </summary>
public sealed class CapEventPublisher(ICapPublisher capPublisher) : IEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync<T>(string name, T payload, CancellationToken cancellationToken = default)
        where T : class =>
        capPublisher.PublishAsync(name, payload, cancellationToken: cancellationToken);
}

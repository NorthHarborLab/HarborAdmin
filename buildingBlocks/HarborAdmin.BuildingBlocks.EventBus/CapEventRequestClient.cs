using DotNetCore.CAP;
using DotNetCore.Cap.RequestReply.Extensions;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// 基于 CAP Request/Reply 的请求客户端。
/// </summary>
public sealed class CapEventRequestClient(ICapPublisher capPublisher, ILogger<CapEventRequestClient> logger) : IEventRequestClient
{
    /// <inheritdoc />
    public Task<TResponse> RequestAsync<TRequest, TResponse>(
        string topic,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        logger.LogInformation(
            "Publishing CAP request {Topic} with request type {RequestType} and response type {ResponseType}.",
            topic,
            typeof(TRequest).FullName,
            typeof(TResponse).FullName);

        return capPublisher.RequestAsync<TRequest, TResponse>(topic, request, timeout, cancellationToken);
    }
}
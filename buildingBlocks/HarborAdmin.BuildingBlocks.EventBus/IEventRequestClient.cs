namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// EventBus Request/Reply 请求客户端。
/// </summary>
public interface IEventRequestClient
{
    /// <summary>
    /// 发布请求消息并等待响应。
    /// </summary>
    Task<TResponse> RequestAsync<TRequest, TResponse>(string topic, TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
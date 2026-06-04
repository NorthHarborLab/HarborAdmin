using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.Client.AI.Clients;

/// <summary>
/// AI 流式调用客户端。
/// </summary>
public interface IAiStreamingClient
{
    /// <summary>
    /// 执行业务 AI 流式请求。
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamAsync(AiBusinessRequest request, CancellationToken cancellationToken = default);
}


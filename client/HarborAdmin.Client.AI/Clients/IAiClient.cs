using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.Client.AI.Clients;

/// <summary>
/// AI 普通调用客户端。
/// </summary>
public interface IAiClient
{
    /// <summary>
    /// 执行业务 AI 请求。
    /// </summary>
    Task<AiBusinessResponse> InvokeAsync(AiBusinessRequest request, CancellationToken cancellationToken = default);
}


using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// AI 供应商适配器。
/// </summary>
public interface IAiProviderAdapter
{
    /// <summary>
    /// 适配器类型。
    /// </summary>
    string AdapterType { get; }

    /// <summary>
    /// 执行非流式调用。
    /// </summary>
    Task<AiProviderCallResult> InvokeAsync(AiProviderCallRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行流式调用。
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamAsync(AiProviderCallRequest request, CancellationToken cancellationToken = default);
}



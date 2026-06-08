using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Chat.Request;

namespace HarborAdmin.Modules.AI.Application.Services;

/// <summary>
/// AI 聊天流服务。
/// </summary>
public sealed class AiChatStreamService(IAiStreamingClient streamingClient)
{
    /// <summary>
    /// 中转 AIWorker SSE 聊天流。
    /// </summary>
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(AiChatStreamRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var invocationId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(request.BusinessKey))
        {
            // SSE 端点不能返回普通 ApiResult，这里用标准 error 事件把校验失败传回前端。
            yield return new AiStreamEvent(
                "error",
                invocationId,
                invocationId,
                1,
                0,
                ErrorCode: AiErrorCodes.InvalidRequest,
                ErrorMessage: "AI 业务 Key 不能为空。");
            yield break;
        }

        // 管理端 Chat 是调试入口，复用同一个 invocationId 作为幂等键与关联 ID，方便 Worker 侧追踪。
        var aiRequest = new AiBusinessRequest(
            request.BusinessKey.Trim(),
            InvocationId: invocationId,
            ProducerKey: NormalizeOptional(request.ProducerKey),
            IdempotencyKey: invocationId,
            Model: NormalizeOptional(request.Model),
            PromptOverride: NormalizeOptional(request.PromptOverride),
            PromptVariables: request.PromptVariables,
            Messages: request.Messages,
            KnowledgeText: NormalizeOptional(request.KnowledgeText),
            KnowledgeTextMode: NormalizeOptional(request.KnowledgeTextMode),
            Metadata: request.Metadata,
            Input: NormalizeOptional(request.Input),
            CorrelationId: invocationId);

        await foreach (var item in streamingClient.StreamAsync(aiRequest, cancellationToken))
        {
            yield return item;
        }
    }

    /// <summary>
    /// 将空白字符串规范化为 <see langword="null"/>。
    /// </summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Requests;

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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

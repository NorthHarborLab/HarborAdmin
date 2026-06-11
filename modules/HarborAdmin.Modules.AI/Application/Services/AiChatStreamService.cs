using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Application.Services.Conversation;
using HarborAdmin.Modules.AI.Contracts.Chat.Request;

namespace HarborAdmin.Modules.AI.Application.Services;

/// <summary>
/// AI 聊天流服务。
/// </summary>
public sealed class AiChatStreamService(
    IAiStreamingClient streamingClient,
    ConversationService conversationService,
    ICurrentUser currentUser)
{
    /// <summary>
    /// 中转 AIWorker SSE 聊天流。
    /// </summary>
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(
        AiChatStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        var conversationId = request.ConversationId;
        var userId = currentUser.Id;
        var userInput = NormalizeOptional(request.Input);
        var assistantContent = string.Empty;
        var streamCompleted = false;
        var streamErrored = false;

        if (conversationId.HasValue && userId > 0)
        {
            var conversation = await conversationService.GetOwnedConversationAsync(userId, conversationId.Value, cancellationToken);
            if (conversation is null)
            {
                yield return new AiStreamEvent(
                    "error",
                    invocationId,
                    invocationId,
                    1,
                    0,
                    ErrorCode: AiErrorCodes.InvalidRequest,
                    ErrorMessage: "会话不存在或无权访问。");
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                await conversationService.AppendUserMessageAsync(userId, conversationId.Value, userInput, cancellationToken);
            }
        }

        var correlationId = conversationId?.ToString() ?? invocationId;

        // 管理端 Chat 是调试入口，复用同一个 invocationId 作为幂等键，关联 ID 优先使用会话 ID。
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
            Input: userInput,
            CorrelationId: correlationId);

        try
        {
            await foreach (var item in streamingClient.StreamAsync(aiRequest, cancellationToken))
            {
                if (item.Type is "delta" or "reasoning_delta")
                {
                    assistantContent += item.Delta ?? string.Empty;
                }
                else if (item.Type == "done")
                {
                    streamCompleted = true;
                }
                else if (item.Type == "error")
                {
                    streamErrored = true;
                }

                yield return item;
            }
        }
        finally
        {
            if (conversationId.HasValue && userId > 0 && !string.IsNullOrWhiteSpace(userInput))
            {
                await PersistAssistantMessageAsync(
                    userId,
                    conversationId.Value,
                    assistantContent,
                    invocationId,
                    streamCompleted,
                    streamErrored,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// 持久化助手消息。
    /// </summary>
    private async Task PersistAssistantMessageAsync(
        long userId,
        long conversationId,
        string content,
        string invocationId,
        bool streamCompleted,
        bool streamErrored,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var isError = streamErrored || !streamCompleted;
        try
        {
            await conversationService.AppendAssistantMessageAsync(
                userId,
                conversationId,
                content,
                invocationId,
                isError,
                cancellationToken);
        }
        catch
        {
            // 持久化失败不影响 SSE 主流程。
        }
    }

    /// <summary>
    /// 将空白字符串规范化为 <see langword="null"/>。
    /// </summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

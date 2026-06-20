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
    IAiClient client,
    IAiStreamingClient streamingClient,
    ConversationService conversationService,
    ICurrentUser currentUser)
{
    /// <summary>
    /// 执行普通 HTTP AI 聊天调用。
    /// </summary>
    public async Task<AiBusinessResponse> InvokeAsync(AiChatStreamRequest request, CancellationToken cancellationToken = default)
    {
        var invocationId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(request.BusinessKey))
        {
            return FailedResponse(request, invocationId, AiErrorCodes.InvalidRequest, "AI 业务 Key 不能为空。");
        }

        var conversationId = request.ConversationId;
        var userId = currentUser.Id;
        var userInput = NormalizeOptional(request.Input);

        if (conversationId.HasValue && userId > 0)
        {
            var conversation = await conversationService.GetOwnedConversationAsync(userId, conversationId.Value, cancellationToken);
            if (conversation is null)
            {
                return FailedResponse(request, invocationId, AiErrorCodes.InvalidRequest, "会话不存在或无权访问。");
            }

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                await conversationService.AppendUserMessageAsync(userId, conversationId.Value, userInput, cancellationToken);
            }
        }

        var aiRequest = BuildAiRequest(request, invocationId, userInput);
        AiBusinessResponse response;
        try
        {
            response = await client.InvokeAsync(aiRequest, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            response = FailedResponse(request, invocationId, "AI_CHAT_INVOKE_FAILED", "AI 聊天调用失败。");
        }

        if (conversationId.HasValue && userId > 0 && !string.IsNullOrWhiteSpace(userInput))
        {
            await PersistAssistantMessageAsync(
                userId,
                conversationId.Value,
                response.Content ?? BuildErrorContent(response),
                response.ReasoningContent,
                response.InvocationId,
                response.Success,
                !response.Success,
                cancellationToken);
        }

        return response;
    }

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
        var assistantReasoningContent = string.Empty;
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

        var aiRequest = BuildAiRequest(request, invocationId, userInput);

        try
        {
            await foreach (var item in streamingClient.StreamAsync(aiRequest, cancellationToken))
            {
                if (item.Type == "delta")
                {
                    assistantContent += item.Delta ?? string.Empty;
                }
                else if (item.Type == "reasoning_delta")
                {
                    assistantReasoningContent += item.Delta ?? string.Empty;
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
                    assistantReasoningContent,
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
        string? reasoningContent,
        string invocationId,
        bool streamCompleted,
        bool streamErrored,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(reasoningContent))
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
                reasoningContent,
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
    /// 构造统一 AI 调用请求。
    /// </summary>
    private static AiBusinessRequest BuildAiRequest(AiChatStreamRequest request, string invocationId, string? userInput)
    {
        var correlationId = request.ConversationId?.ToString() ?? invocationId;
        // 管理端 Chat 是调试入口，复用同一个 invocationId 作为幂等键，关联 ID 优先使用会话 ID。
        return new AiBusinessRequest(
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
    }

    /// <summary>
    /// 构造聊天失败响应。
    /// </summary>
    private static AiBusinessResponse FailedResponse(AiChatStreamRequest request, string invocationId, string errorCode, string errorMessage) =>
        new(false, invocationId, request.ConversationId?.ToString() ?? invocationId, "Failed", 0, ErrorCode: errorCode, ErrorMessage: errorMessage);

    /// <summary>
    /// 构造用于会话记录的失败正文。
    /// </summary>
    private static string BuildErrorContent(AiBusinessResponse response) =>
        response.Success ? string.Empty : $"{response.ErrorCode ?? "AI_ERROR"}: {response.ErrorMessage ?? string.Empty}".Trim();

    /// <summary>
    /// 将空白字符串规范化为 <see langword="null"/>。
    /// </summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using System.Text.Encodings.Web;
using System.Text.Json;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 聊天调试 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/chat")]
public sealed class AiChatController(IAiStreamingClient streamingClient) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 中转 AIWorker SSE 聊天流。
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream([FromBody] AiChatStreamRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var invocationId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(request.BusinessKey))
        {
            await WriteEventAsync(
                new AiStreamEvent("error", invocationId, invocationId, 1, 0, ErrorCode: AiErrorCodes.InvalidRequest, ErrorMessage: "AI business key is required."),
                cancellationToken);
            return;
        }

        try
        {
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
                await WriteEventAsync(item, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 浏览器主动中断 SSE 时无需再写响应。
        }
        catch
        {
            await WriteEventAsync(
                new AiStreamEvent("error", invocationId, invocationId, 1, 0, ErrorCode: "AI_CHAT_STREAM_FAILED", ErrorMessage: "AI chat stream failed."),
                cancellationToken);
        }
    }

    private async Task WriteEventAsync(AiStreamEvent item, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {item.Type}\n", cancellationToken);
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(item, JsonOptions)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

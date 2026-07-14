using System.Text.Encodings.Web;
using System.Text.Json;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Application.Services.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.Chat.Dto;
using HarborAdmin.Modules.AI.Contracts.Chat.Request;

namespace HarborAdmin.Modules.AI.Controllers.Chat;

/// <summary>
/// AI 聊天调试 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/chat")]
public sealed class ChatController(AiChatStreamService chatStreamService, BusinessService businessService) : AdminControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 列出可用于聊天调用的业务选项。
    /// </summary>
    [HttpGet("businesses")]
    public async Task<ApiResult<IReadOnlyList<AiChatBusinessOptionDto>>> ListBusinesses(CancellationToken cancellationToken) =>
        ApiResult.Ok(await businessService.ListChatOptionsAsync(cancellationToken));

    /// <summary>
    /// 执行普通 HTTP AI 聊天调用。
    /// </summary>
    [HttpPost("invoke")]
    public async Task<ApiResult<AiBusinessResponse>> Invoke([FromBody] AiChatStreamRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await chatStreamService.InvokeAsync(request, cancellationToken));

    /// <summary>
    /// 中转 AIWorker SSE 聊天流。
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream([FromBody] AiChatStreamRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        // 禁用 Nginx 代理缓冲，让 AIWorker 的 token 流可以及时透传到浏览器。
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var item in chatStreamService.StreamAsync(request, cancellationToken))
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
            var invocationId = Guid.NewGuid().ToString("N");
            await WriteEventAsync(
                new AiStreamEvent("error", invocationId, invocationId, 1, 0, ErrorCode: "AI_CHAT_STREAM_FAILED", ErrorMessage: "AI 聊天流转发失败。"),
                cancellationToken);
        }
    }

    /// <summary>
    /// 按 SSE 格式写入单个 AI 流事件。
    /// </summary>
    private async Task WriteEventAsync(AiStreamEvent item, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {item.Type}\n", cancellationToken);
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(item, JsonOptions)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}

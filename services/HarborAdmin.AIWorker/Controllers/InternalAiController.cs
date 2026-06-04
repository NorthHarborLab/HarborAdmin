using System.Text.Json;
using HarborAdmin.AIWorker.Application;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace HarborAdmin.AIWorker.Controllers;

/// <summary>
/// AIWorker 内部调用 API。
/// </summary>
[ApiController]
[Route("internal/ai")]
public sealed class InternalAiController(
    AiRequestSignatureValidator signatureValidator,
    AiExecutionService executionService,
    IOptions<MvcJsonOptions> jsonOptions) : ControllerBase
{
    /// <summary>
    /// 执行非流式 AI 调用。
    /// </summary>
    [HttpPost("invoke")]
    public async Task<IActionResult> Invoke(CancellationToken cancellationToken)
    {
        var read = await ReadRequestAsync(cancellationToken);
        if (read.Request is null)
        {
            return StatusCode(
                StatusCodes.Status400BadRequest,
                ErrorResponse(null, AiErrorCodes.InvalidRequest, "AI request body is invalid."));
        }

        var signature = await signatureValidator.ValidateAsync(Request, read.Body, read.Request, cancellationToken);
        if (!signature.Valid)
        {
            return StatusCode(
                StatusCodes.Status401Unauthorized,
                ErrorResponse(read.Request, signature.ErrorCode!, signature.ErrorMessage!));
        }

        var response = await executionService.InvokeAsync(read.Request, cancellationToken);
        return StatusCode(
            response.Success ? StatusCodes.Status200OK : StatusCodes.Status422UnprocessableEntity,
            response);
    }

    /// <summary>
    /// 执行流式 AI 调用。
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        var read = await ReadRequestAsync(cancellationToken);
        if (read.Request is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                ErrorResponse(null, AiErrorCodes.InvalidRequest, "AI request body is invalid."),
                JsonSerializerOptions,
                cancellationToken);
            return;
        }

        var signature = await signatureValidator.ValidateAsync(Request, read.Body, read.Request, cancellationToken);
        if (!signature.Valid)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(
                ErrorResponse(read.Request, signature.ErrorCode!, signature.ErrorMessage!),
                JsonSerializerOptions,
                cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var item in executionService.StreamAsync(read.Request, cancellationToken))
        {
            await Response.WriteAsync($"event: {item.Type}\n", cancellationToken);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(item, JsonSerializerOptions)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private JsonSerializerOptions JsonSerializerOptions => jsonOptions.Value.JsonSerializerOptions;

    private async Task<AiRequestReadResult> ReadRequestAsync(CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        var body = memoryStream.ToArray();
        try
        {
            return new AiRequestReadResult(JsonSerializer.Deserialize<AiBusinessRequest>(body, JsonSerializerOptions), body);
        }
        catch (JsonException)
        {
            return new AiRequestReadResult(null, body);
        }
    }

    private static AiBusinessResponse ErrorResponse(AiBusinessRequest? request, string errorCode, string errorMessage)
    {
        var invocationId = string.IsNullOrWhiteSpace(request?.InvocationId) ? Guid.NewGuid().ToString("N") : request.InvocationId;
        var correlationId = string.IsNullOrWhiteSpace(request?.CorrelationId) ? invocationId : request.CorrelationId;
        return new AiBusinessResponse(false, invocationId, correlationId, "Failed", 0, ErrorCode: errorCode, ErrorMessage: errorMessage);
    }

    private sealed record AiRequestReadResult(AiBusinessRequest? Request, byte[] Body);
}

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Constants;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// OpenAI Responses 适配器。
/// </summary>
public sealed class OpenAiResponsesAdapter(HttpClient httpClient) : IAiProviderAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public string AdapterType => AiAdapterTypes.OpenAiResponses;

    /// <inheritdoc />
    public async Task<AiProviderCallResult> InvokeAsync(AiProviderCallRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = BuildRequest(request, streaming: false);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AiProviderException(response.StatusCode, body);
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        return new AiProviderCallResult(
            ExtractOutputText(root),
            OpenAiChatCompletionsAdapter.ParseUsage(root),
            response.Headers.TryGetValues("x-request-id", out var values) ? values.FirstOrDefault() : OpenAiChatCompletionsAdapter.GetString(root, "id"),
            OpenAiChatCompletionsAdapter.GetString(root, "status"),
            null,
            CountToolCalls(root));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(
        AiProviderCallRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var httpRequest = BuildRequest(request, streaming: true);
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiProviderException(response.StatusCode, body);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        string? eventName = null;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                eventName = null;
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line[6..].Trim();
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                yield return new AiStreamEvent("done", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                    ProviderKey: request.Provider.ProviderKey, Model: request.Model);
                yield break;
            }

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            var type = OpenAiChatCompletionsAdapter.GetString(root, "type") ?? eventName;
            if (string.Equals(type, "response.output_text.delta", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "response.output_text.annotation.added", StringComparison.OrdinalIgnoreCase))
            {
                var delta = OpenAiChatCompletionsAdapter.GetString(root, "delta");
                if (!string.IsNullOrEmpty(delta))
                {
                    yield return new AiStreamEvent("delta", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion, Delta: delta,
                        ProviderKey: request.Provider.ProviderKey, Model: request.Model);
                }
            }
            else if (string.Equals(type, "response.completed", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("response", out var completed))
                {
                    yield return new AiStreamEvent("usage", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                        ProviderKey: request.Provider.ProviderKey, Model: request.Model,
                        Usage: OpenAiChatCompletionsAdapter.ParseUsage(completed));
                }

                yield return new AiStreamEvent("done", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                    ProviderKey: request.Provider.ProviderKey, Model: request.Model);
                yield break;
            }
            else if (type?.Contains("function_call", StringComparison.OrdinalIgnoreCase) == true)
            {
                yield return new AiStreamEvent("tool_call", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                    ProviderKey: request.Provider.ProviderKey, Model: request.Model,
                    ToolCall: new AiToolCall(OpenAiChatCompletionsAdapter.GetString(root, "item_id") ?? string.Empty,
                        OpenAiChatCompletionsAdapter.GetString(root, "name") ?? string.Empty,
                        OpenAiChatCompletionsAdapter.GetString(root, "arguments") ?? string.Empty));
            }
        }
    }

    private static HttpRequestMessage BuildRequest(AiProviderCallRequest request, bool streaming)
    {
        var uri = new Uri(new Uri(request.Provider.BaseUrl.TrimEnd('/') + "/"), "responses");
        var body = new JsonObject
        {
            ["model"] = request.Model,
            ["stream"] = streaming,
            ["input"] = new JsonArray(request.Messages.Select(ToResponsesInput).ToArray<JsonNode?>())
        };
        ApplyResponsesOutputOptions(body, request.OutputOptions);
        ApplyResponsesToolOptions(body, request.ToolOptions);
        OpenAiChatCompletionsAdapter.ApplyProviderOptions(body, request.ProviderOptions);
        OpenAiChatCompletionsAdapter.MergeJson(body, request.Provider.DefaultBodyJson);
        OpenAiChatCompletionsAdapter.MergeJson(body, request.RouteProviderOptionsJson);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(request.ResolvedSecret))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ResolvedSecret.Trim());
        }

        OpenAiChatCompletionsAdapter.ApplyHeaders(httpRequest, request.Provider.DefaultHeadersJson);
        return httpRequest;
    }

    private static JsonObject ToResponsesInput(AiMessage message) =>
        new()
        {
            ["role"] = message.Role == "system" ? "developer" : message.Role,
            ["content"] = new JsonArray((message.Parts is { Count: > 0 }
                ? message.Parts.Select(ToResponsesPart)
                : [new JsonObject { ["type"] = "input_text", ["text"] = message.Content ?? string.Empty }]).ToArray<JsonNode?>())
        };

    private static JsonObject ToResponsesPart(AiMessageContentPart part) =>
        part.Type switch
        {
            "image_url" => new JsonObject { ["type"] = "input_image", ["image_url"] = part.Url ?? part.FileUri ?? string.Empty },
            "file_uri" => new JsonObject { ["type"] = "input_file", ["file_id"] = part.FileUri ?? string.Empty },
            _ => new JsonObject { ["type"] = "input_text", ["text"] = part.Text ?? part.ResultJson ?? string.Empty }
        };

    private static void ApplyResponsesOutputOptions(JsonObject body, AiOutputOptions? options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.ResponseFormat))
        {
            return;
        }

        if (string.Equals(options.ResponseFormat, "json_schema", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(options.JsonSchema))
        {
            body["text"] = new JsonObject
            {
                ["format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["name"] = "ai_response",
                    ["strict"] = options.Strict,
                    ["schema"] = JsonNode.Parse(options.JsonSchema)
                }
            };
        }
        else if (string.Equals(options.ResponseFormat, "json_object", StringComparison.OrdinalIgnoreCase))
        {
            body["text"] = new JsonObject { ["format"] = new JsonObject { ["type"] = "json_object" } };
        }
    }

    private static void ApplyResponsesToolOptions(JsonObject body, AiToolOptions? options)
    {
        if (options?.Tools is not { Count: > 0 })
        {
            return;
        }

        body["tools"] = new JsonArray(options.Tools.Select(tool => new JsonObject
        {
            ["type"] = "function",
            ["name"] = tool.Name,
            ["description"] = tool.Description,
            ["parameters"] = string.IsNullOrWhiteSpace(tool.ParametersJson) ? new JsonObject { ["type"] = "object" } : JsonNode.Parse(tool.ParametersJson)
        }).ToArray<JsonNode?>());
        if (!string.IsNullOrWhiteSpace(options.ToolChoice))
        {
            body["tool_choice"] = options.ToolChoice;
        }
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.ToString();
    }

    private static int CountToolCalls(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return output.EnumerateArray().Count(item => string.Equals(OpenAiChatCompletionsAdapter.GetString(item, "type"), "function_call", StringComparison.OrdinalIgnoreCase));
    }
}



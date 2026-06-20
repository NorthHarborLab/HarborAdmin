using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Shared.Constant;

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
            CountToolCalls(root),
            ExtractReasoningText(root));
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
            else if (IsReasoningDelta(type))
            {
                var delta = OpenAiChatCompletionsAdapter.GetString(root, "delta") ?? OpenAiChatCompletionsAdapter.GetString(root, "text");
                if (!string.IsNullOrEmpty(delta))
                {
                    yield return new AiStreamEvent("reasoning_delta", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion, Delta: delta,
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

    /// <summary>
    /// 构造 OpenAI Responses 请求。
    /// </summary>
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

    /// <summary>
    /// 转换统一消息为 Responses input 项。
    /// </summary>
    private static JsonObject ToResponsesInput(AiMessage message) =>
        new()
        {
            ["role"] = message.Role == "system" ? "developer" : message.Role,
            ["content"] = new JsonArray((message.Parts is { Count: > 0 }
                ? message.Parts.Select(ToResponsesPart)
                : [new JsonObject { ["type"] = "input_text", ["text"] = message.Content ?? string.Empty }]).ToArray<JsonNode?>())
        };

    /// <summary>
    /// 转换统一多模态内容块为 Responses content part。
    /// </summary>
    private static JsonObject ToResponsesPart(AiMessageContentPart part) =>
        part.Type switch
        {
            "image_url" => new JsonObject { ["type"] = "input_image", ["image_url"] = part.Url ?? part.FileUri ?? string.Empty },
            "file_uri" => new JsonObject { ["type"] = "input_file", ["file_id"] = part.FileUri ?? string.Empty },
            _ => new JsonObject { ["type"] = "input_text", ["text"] = part.Text ?? part.ResultJson ?? string.Empty }
        };

    /// <summary>
    /// 应用 Responses 结构化输出选项。
    /// </summary>
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

    /// <summary>
    /// 应用 Responses 工具调用选项。
    /// </summary>
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

    /// <summary>
    /// 从 Responses 响应中提取最终文本。
    /// </summary>
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

    /// <summary>
    /// 从 Responses 响应中提取思考文本。
    /// </summary>
    private static string? ExtractReasoningText(JsonElement root)
    {
        var direct = OpenAiChatCompletionsAdapter.GetString(root, "reasoning_content");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (root.TryGetProperty("reasoning", out var reasoning))
        {
            if (reasoning.ValueKind == JsonValueKind.String)
            {
                return reasoning.GetString();
            }

            var reasoningBuilder = new StringBuilder();
            AppendTextFragments(reasoningBuilder, reasoning, "summary");
            AppendTextFragments(reasoningBuilder, reasoning, "content");
            AppendStringProperty(reasoningBuilder, reasoning, "text");
            if (reasoningBuilder.Length > 0)
            {
                return reasoningBuilder.ToString();
            }
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            var type = OpenAiChatCompletionsAdapter.GetString(item, "type");
            if (type?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) != true)
            {
                continue;
            }

            AppendTextFragments(builder, item, "summary");
            AppendTextFragments(builder, item, "content");
            AppendStringProperty(builder, item, "text");
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    /// <summary>
    /// 判断 Responses 事件是否为思考增量。
    /// </summary>
    private static bool IsReasoningDelta(string? type) =>
        type?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true &&
        (type.EndsWith(".delta", StringComparison.OrdinalIgnoreCase) ||
         type.Contains("summary_text.delta", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 拼接数组属性中的文本片段。
    /// </summary>
    private static void AppendTextFragments(StringBuilder builder, JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var fragments) || fragments.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var fragment in fragments.EnumerateArray())
        {
            if (fragment.ValueKind == JsonValueKind.String)
            {
                builder.Append(fragment.GetString());
            }
            else
            {
                AppendStringProperty(builder, fragment, "text");
            }
        }
    }

    /// <summary>
    /// 拼接字符串属性。
    /// </summary>
    private static void AppendStringProperty(StringBuilder builder, JsonElement element, string property)
    {
        var value = OpenAiChatCompletionsAdapter.GetString(element, property);
        if (!string.IsNullOrEmpty(value))
        {
            builder.Append(value);
        }
    }

    /// <summary>
    /// 统计 Responses 输出中的工具调用数量。
    /// </summary>
    private static int CountToolCalls(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return output.EnumerateArray().Count(item => string.Equals(OpenAiChatCompletionsAdapter.GetString(item, "type"), "function_call", StringComparison.OrdinalIgnoreCase));
    }
}



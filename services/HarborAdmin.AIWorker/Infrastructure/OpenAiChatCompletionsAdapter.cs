using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Shared.Constant;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// OpenAI Chat Completions 兼容适配器。
/// </summary>
public sealed class OpenAiChatCompletionsAdapter(HttpClient httpClient) : IAiProviderAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public string AdapterType => AiAdapterTypes.OpenAiChatCompletions;

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
        var choice = root.GetProperty("choices")[0];
        var message = choice.GetProperty("message");
        var content = message.TryGetProperty("content", out var contentElement) ? contentElement.GetString() ?? string.Empty : string.Empty;
        var reasoningContent = GetString(message, "reasoning_content");
        var toolCallCount = message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array ? toolCalls.GetArrayLength() : 0;
        var providerRequestId = ReadHeader(response, "x-request-id") ?? ReadHeader(response, "x-request-id".ToUpperInvariant()) ?? GetString(root, "id");
        return new AiProviderCallResult(content, ParseUsage(root), providerRequestId, GetString(choice, "finish_reason"), GetString(root, "provider"), toolCallCount,
            reasoningContent);
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
        string? finalFinishReason = null;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                yield return new AiStreamEvent("done", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                    ProviderKey: request.Provider.ProviderKey, Model: request.Model, FinishReason: finalFinishReason);
                yield break;
            }

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta))
            {
                if (delta.TryGetProperty("content", out var contentElement))
                {
                    var deltaText = contentElement.GetString();
                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        yield return new AiStreamEvent("delta", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion, Delta: deltaText,
                            ProviderKey: request.Provider.ProviderKey, Model: request.Model);
                    }
                }

                if (delta.TryGetProperty("reasoning_content", out var reasoningElement))
                {
                    var reasoning = reasoningElement.GetString();
                    if (!string.IsNullOrEmpty(reasoning))
                    {
                        yield return new AiStreamEvent("reasoning_delta", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion, Delta: reasoning,
                            ProviderKey: request.Provider.ProviderKey, Model: request.Model);
                    }
                }

                if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
                {
                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        var function = toolCall.TryGetProperty("function", out var fn) ? fn : default;
                        yield return new AiStreamEvent("tool_call", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                            ProviderKey: request.Provider.ProviderKey, Model: request.Model,
                            ToolCall: new AiToolCall(GetString(toolCall, "id") ?? string.Empty, GetString(function, "name") ?? string.Empty,
                                GetString(function, "arguments") ?? string.Empty));
                    }
                }

                var finishReason = GetString(choices[0], "finish_reason");
                if (!string.IsNullOrWhiteSpace(finishReason))
                {
                    finalFinishReason = finishReason;
                }
            }

            if (root.TryGetProperty("usage", out var usageElement))
            {
                yield return new AiStreamEvent("usage", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
                    ProviderKey: request.Provider.ProviderKey, Model: request.Model, Usage: ParseUsage(usageElement));
            }
        }
    }

    /// <summary>
    /// 构造 OpenAI Chat Completions 请求。
    /// </summary>
    private static HttpRequestMessage BuildRequest(AiProviderCallRequest request, bool streaming)
    {
        var uri = new Uri(new Uri(request.Provider.BaseUrl.TrimEnd('/') + "/"), "chat/completions");
        var body = new JsonObject
        {
            ["model"] = request.Model,
            ["stream"] = streaming,
            ["messages"] = new JsonArray(request.Messages.Select(ToOpenAiMessage).ToArray<JsonNode?>())
        };
        ApplyOutputOptions(body, request.OutputOptions);
        ApplyToolOptions(body, request.ToolOptions);
        ApplyProviderOptions(body, request.ProviderOptions);
        // 后写入的 JSON 覆盖前面的默认字段，允许路由和 OpenRouter 选项做更细粒度调整。
        MergeJson(body, request.Provider.DefaultBodyJson);
        MergeJson(body, request.RouteProviderOptionsJson);
        MergeJson(body, request.OpenRouterOptionsJson);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
        };
        ApplyAuthentication(httpRequest, request.ResolvedSecret);
        ApplyHeaders(httpRequest, request.Provider.DefaultHeadersJson);
        return httpRequest;
    }

    /// <summary>
    /// 添加 Bearer API Key。
    /// </summary>
    private static void ApplyAuthentication(HttpRequestMessage request, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
    }

    /// <summary>
    /// 应用供应商默认请求头。
    /// </summary>
    internal static void ApplyHeaders(HttpRequestMessage request, string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson))
        {
            return;
        }

        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson, JsonOptions) ?? [];
        foreach (var (key, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    /// <summary>
    /// 将扩展 JSON 合并到请求体。
    /// </summary>
    internal static void MergeJson(JsonObject target, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var extra = JsonNode.Parse(json) as JsonObject;
        if (extra is null)
        {
            return;
        }

        foreach (var (key, value) in extra)
        {
            target[key] = value?.DeepClone();
        }
    }

    /// <summary>
    /// 转换统一消息为 OpenAI Chat 消息。
    /// </summary>
    internal static JsonObject ToOpenAiMessage(AiMessage message)
    {
        var result = new JsonObject { ["role"] = message.Role };
        if (message.Parts is { Count: > 0 })
        {
            result["content"] = new JsonArray(message.Parts.Select(ToOpenAiContentPart).ToArray<JsonNode?>());
        }
        else
        {
            result["content"] = message.Content ?? string.Empty;
        }

        return result;
    }

    /// <summary>
    /// 转换统一多模态内容块为 OpenAI Chat 内容块。
    /// </summary>
    private static JsonObject ToOpenAiContentPart(AiMessageContentPart part) =>
        part.Type switch
        {
            "image_url" => new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject { ["url"] = part.Url ?? part.FileUri ?? string.Empty }
            },
            "file_uri" => new JsonObject
            {
                ["type"] = "file",
                ["file"] = new JsonObject { ["file_id"] = part.FileUri ?? string.Empty }
            },
            _ => new JsonObject { ["type"] = "text", ["text"] = part.Text ?? part.ResultJson ?? string.Empty }
        };

    /// <summary>
    /// 应用结构化输出选项。
    /// </summary>
    internal static void ApplyOutputOptions(JsonObject body, AiOutputOptions? options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.ResponseFormat))
        {
            return;
        }

        if (string.Equals(options.ResponseFormat, "json_schema", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(options.JsonSchema))
        {
            body["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = "ai_response",
                    ["strict"] = options.Strict,
                    ["schema"] = JsonNode.Parse(options.JsonSchema)
                }
            };
            return;
        }

        if (string.Equals(options.ResponseFormat, "json_object", StringComparison.OrdinalIgnoreCase))
        {
            body["response_format"] = new JsonObject { ["type"] = "json_object" };
        }
    }

    /// <summary>
    /// 应用工具调用选项。
    /// </summary>
    internal static void ApplyToolOptions(JsonObject body, AiToolOptions? options)
    {
        if (options?.Tools is not { Count: > 0 })
        {
            return;
        }

        body["tools"] = new JsonArray(options.Tools.Select(tool => new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = string.IsNullOrWhiteSpace(tool.ParametersJson) ? new JsonObject { ["type"] = "object" } : JsonNode.Parse(tool.ParametersJson)
            }
        }).ToArray<JsonNode?>());
        if (!string.IsNullOrWhiteSpace(options.ToolChoice))
        {
            body["tool_choice"] = options.ToolChoice;
        }
    }

    /// <summary>
    /// 应用通用供应商采样与额外请求体选项。
    /// </summary>
    internal static void ApplyProviderOptions(JsonObject body, AiProviderOptions? options)
    {
        if (options is null)
        {
            return;
        }

        if (options.Temperature is not null)
        {
            body["temperature"] = options.Temperature;
        }

        if (options.TopP is not null)
        {
            body["top_p"] = options.TopP;
        }

        if (options.MaxTokens is not null)
        {
            body["max_tokens"] = options.MaxTokens;
        }

        if (options.Stop is { Count: > 0 })
        {
            body["stop"] = new JsonArray(options.Stop.Select(stop => JsonValue.Create(stop)).ToArray<JsonNode?>());
        }

        if (!string.IsNullOrWhiteSpace(options.ReasoningEffort))
        {
            body["reasoning_effort"] = options.ReasoningEffort;
        }

        MergeJson(body, options.ExtraBodyJson);
    }

    /// <summary>
    /// 从 OpenAI 兼容响应中解析用量。
    /// </summary>
    internal static AiUsage ParseUsage(JsonElement root)
    {
        var usage = root.TryGetProperty("usage", out var usageElement) ? usageElement : root;
        var prompt = GetInt(usage, "prompt_tokens");
        var completion = GetInt(usage, "completion_tokens");
        var total = GetInt(usage, "total_tokens");
        var reasoning = GetInt(usage, "reasoning_tokens");
        var cached = GetInt(usage, "cached_tokens");
        if (usage.TryGetProperty("completion_tokens_details", out var completionDetails))
        {
            reasoning = Math.Max(reasoning, GetInt(completionDetails, "reasoning_tokens"));
        }

        if (usage.TryGetProperty("prompt_tokens_details", out var promptDetails))
        {
            cached = Math.Max(cached, GetInt(promptDetails, "cached_tokens"));
        }

        return new AiUsage(prompt, completion, total == 0 ? prompt + completion : total, reasoning, cached,
            GetInt(usage, "native_tokens_prompt"), GetInt(usage, "native_tokens_completion"), GetDecimal(usage, "cost"));
    }

    /// <summary>
    /// 读取整数属性，不存在时返回 0。
    /// </summary>
    internal static int GetInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : 0;

    /// <summary>
    /// 读取 decimal 属性，不存在时返回 0。
    /// </summary>
    internal static decimal GetDecimal(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetDecimal(out var result) ? result : 0;

    /// <summary>
    /// 读取字符串属性。
    /// </summary>
    internal static string? GetString(JsonElement element, string property) =>
        element.ValueKind != JsonValueKind.Undefined && element.TryGetProperty(property, out var value) ? value.GetString() : null;

    /// <summary>
    /// 读取响应头的第一个值。
    /// </summary>
    private static string? ReadHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
}



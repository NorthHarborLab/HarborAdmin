using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Constants;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// Google Gemini generateContent 适配器。
/// </summary>
public sealed class GoogleGeminiGenerateContentAdapter(HttpClient httpClient) : IAiProviderAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public string AdapterType => AiAdapterTypes.GoogleGeminiGenerateContent;

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
        var candidate = root.GetProperty("candidates")[0];
        var content = ReadGeminiText(candidate);
        var usage = root.TryGetProperty("usageMetadata", out var usageElement)
            ? new AiUsage(GetInt(usageElement, "promptTokenCount"), GetInt(usageElement, "candidatesTokenCount"), GetInt(usageElement, "totalTokenCount"))
            : new AiUsage();
        return new AiProviderCallResult(content, usage, null, GetString(candidate, "finishReason"));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(
        AiProviderCallRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var httpRequest = BuildRequest(request, streaming: true);
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AiProviderException(response.StatusCode, body);
        }

        using var document = JsonDocument.Parse(body);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var text = ReadGeminiText(candidates[0]);
                if (!string.IsNullOrEmpty(text))
                {
                    yield return new AiStreamEvent("delta", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion, Delta: text,
                        ProviderKey: request.Provider.ProviderKey, Model: request.Model);
                }
            }
        }

        yield return new AiStreamEvent("done", request.InvocationId, request.CorrelationId, 0, request.ReleaseVersion,
            ProviderKey: request.Provider.ProviderKey, Model: request.Model);
    }

    private static HttpRequestMessage BuildRequest(AiProviderCallRequest request, bool streaming)
    {
        var suffix = streaming ? $"models/{request.Model}:streamGenerateContent" : $"models/{request.Model}:generateContent";
        var uriBuilder = new UriBuilder(new Uri(new Uri(request.Provider.BaseUrl.TrimEnd('/') + "/"), suffix));
        if (!string.IsNullOrWhiteSpace(request.ResolvedSecret))
        {
            uriBuilder.Query = $"key={Uri.EscapeDataString(request.ResolvedSecret.Trim())}";
        }

        var body = new JsonObject
        {
            ["contents"] = new JsonArray(request.Messages.Where(m => m.Role != "system").Select(ToGeminiContent).ToArray<JsonNode?>())
        };
        var systemInstruction = string.Join("\n\n", request.Messages.Where(m => m.Role == "system").Select(m => m.Content).Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            body["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = systemInstruction }) };
        }

        ApplyGeminiOutputOptions(body, request.OutputOptions);
        ApplyGeminiToolOptions(body, request.ToolOptions);
        ApplyGeminiProviderOptions(body, request.ProviderOptions);
        OpenAiChatCompletionsAdapter.MergeJson(body, request.Provider.DefaultBodyJson);
        OpenAiChatCompletionsAdapter.MergeJson(body, request.RouteProviderOptionsJson);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri)
        {
            Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
        };
        OpenAiChatCompletionsAdapter.ApplyHeaders(httpRequest, request.Provider.DefaultHeadersJson);
        return httpRequest;
    }

    private static int GetInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : 0;

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind != JsonValueKind.Undefined && element.TryGetProperty(property, out var value) ? value.GetString() : null;

    private static string ReadGeminiText(JsonElement candidate)
    {
        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        return string.Concat(parts.EnumerateArray()
            .Where(part => part.TryGetProperty("text", out _))
            .Select(part => part.GetProperty("text").GetString()));
    }

    private static JsonObject ToGeminiContent(AiMessage message) =>
        new()
        {
            ["role"] = message.Role == "assistant" ? "model" : "user",
            ["parts"] = new JsonArray((message.Parts is { Count: > 0 }
                ? message.Parts.Select(ToGeminiPart)
                : [new JsonObject { ["text"] = message.Content ?? string.Empty }]).ToArray<JsonNode?>())
        };

    private static JsonObject ToGeminiPart(AiMessageContentPart part)
    {
        if (part.Type is "file_uri" or "image_url" or "audio" or "video")
        {
            return new JsonObject
            {
                ["file_data"] = new JsonObject
                {
                    ["mime_type"] = part.MimeType ?? "application/octet-stream",
                    ["file_uri"] = part.FileUri ?? part.Url ?? string.Empty
                }
            };
        }

        return new JsonObject { ["text"] = part.Text ?? part.ResultJson ?? string.Empty };
    }

    private static void ApplyGeminiOutputOptions(JsonObject body, AiOutputOptions? options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.ResponseFormat))
        {
            return;
        }

        var generationConfig = GetOrCreateObject(body, "generationConfig");
        if (string.Equals(options.ResponseFormat, "json_schema", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(options.JsonSchema))
        {
            generationConfig["responseMimeType"] = "application/json";
            generationConfig["responseJsonSchema"] = JsonNode.Parse(options.JsonSchema);
        }
        else if (string.Equals(options.ResponseFormat, "json_object", StringComparison.OrdinalIgnoreCase))
        {
            generationConfig["responseMimeType"] = "application/json";
        }
    }

    private static void ApplyGeminiToolOptions(JsonObject body, AiToolOptions? options)
    {
        if (options?.Tools is not { Count: > 0 })
        {
            return;
        }

        body["tools"] = new JsonArray(new JsonObject
        {
            ["functionDeclarations"] = new JsonArray(options.Tools.Select(tool => new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = string.IsNullOrWhiteSpace(tool.ParametersJson) ? new JsonObject { ["type"] = "object" } : JsonNode.Parse(tool.ParametersJson)
            }).ToArray<JsonNode?>())
        });
    }

    private static void ApplyGeminiProviderOptions(JsonObject body, AiProviderOptions? options)
    {
        if (options is null)
        {
            return;
        }

        var generationConfig = GetOrCreateObject(body, "generationConfig");
        if (options.Temperature is not null)
        {
            generationConfig["temperature"] = options.Temperature;
        }

        if (options.TopP is not null)
        {
            generationConfig["topP"] = options.TopP;
        }

        if (options.MaxTokens is not null)
        {
            generationConfig["maxOutputTokens"] = options.MaxTokens;
        }

        OpenAiChatCompletionsAdapter.MergeJson(generationConfig, options.ExtraBodyJson);
    }

    private static JsonObject GetOrCreateObject(JsonObject body, string key)
    {
        if (body[key] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        body[key] = created;
        return created;
    }
}



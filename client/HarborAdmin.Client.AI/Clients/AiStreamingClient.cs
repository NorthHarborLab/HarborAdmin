using System.Runtime.CompilerServices;
using System.Text.Json;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Client.AI.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Client.AI.Clients;

/// <summary>
/// 基于 AIWorker SSE 的 AI 流式客户端。
/// </summary>
public sealed class AiStreamingClient(HttpClient httpClient, IOptions<AiOptions> options, AiRequestSigner signer) : IAiStreamingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(AiBusinessRequest request,[EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.Value.StreamingTimeoutSeconds)));
        var uri = new Uri(new Uri(options.Value.WorkerBaseUrl.TrimEnd('/') + "/"), "internal/ai/stream");
        using var httpRequest = await signer.CreateSignedRequestAsync(HttpMethod.Post, uri, request, timeoutCts.Token);
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
        using var reader = new StreamReader(stream);

        string? eventName = null;
        var data = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(timeoutCts.Token)) is not null)
        {
            if (string.IsNullOrEmpty(line))
            {
                if (data.Count > 0)
                {
                    var payload = string.Join('\n', data);
                    var parsed = JsonSerializer.Deserialize<AiStreamEvent>(payload, JsonOptions);
                    if (parsed is not null)
                    {
                        yield return parsed with { Type = eventName ?? parsed.Type };
                    }
                }

                eventName = null;
                data.Clear();
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line[6..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                data.Add(line[5..].TrimStart());
            }
        }
    }
}
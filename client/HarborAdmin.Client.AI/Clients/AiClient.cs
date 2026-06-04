using System.Net.Http.Json;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Client.AI.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Client.AI.Clients;

/// <summary>
/// 基于 AIWorker HTTP 的 AI 客户端。
/// </summary>
public sealed class AiClient(HttpClient httpClient, IOptions<AiOptions> options, AiRequestSigner signer) : IAiClient
{
    /// <inheritdoc />
    public async Task<AiBusinessResponse> InvokeAsync(AiBusinessRequest request, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.Value.RequestTimeoutSeconds)));
        var uri = BuildUri("internal/ai/invoke");
        using var httpRequest = await signer.CreateSignedRequestAsync(HttpMethod.Post, uri, request, timeoutCts.Token);
        using var response = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
        var result = await response.Content.ReadFromJsonAsync<AiBusinessResponse>(cancellationToken: timeoutCts.Token);
        if (result is null)
        {
            response.EnsureSuccessStatusCode();
            throw new InvalidOperationException("AIWorker returned an empty response.");
        }

        return result;
    }

    private Uri BuildUri(string path) =>
        new(new Uri(options.Value.WorkerBaseUrl.TrimEnd('/') + "/"), path);
}

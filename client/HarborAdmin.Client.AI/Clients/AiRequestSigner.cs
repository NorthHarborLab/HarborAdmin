using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Client.AI.Options;
using Microsoft.Extensions.Options;

namespace HarborAdmin.Client.AI.Clients;

/// <summary>
/// AIWorker 内部请求签名器。
/// </summary>
public sealed class AiRequestSigner(
    IOptions<AiOptions> options,
    IEnumerable<ISecretResolver> secretResolvers,
    IAiBusinessSigningSecretResolver? businessSigningSecretResolver = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 创建已签名 HTTP 请求
    /// </summary>
    public async Task<HttpRequestMessage> CreateSignedRequestAsync(HttpMethod method, Uri uri, AiBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        var invocationId = string.IsNullOrWhiteSpace(request.InvocationId) ? Guid.NewGuid().ToString("N") : request.InvocationId.Trim();
        var producerKey = string.IsNullOrWhiteSpace(request.ProducerKey) ? options.Value.ProducerKey.Trim() : request.ProducerKey.Trim();
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? invocationId : request.IdempotencyKey.Trim();
        var enriched = request with { InvocationId = invocationId, ProducerKey = producerKey, IdempotencyKey = idempotencyKey };
        var body = JsonSerializer.SerializeToUtf8Bytes(enriched, JsonOptions);
        var message = new HttpRequestMessage(method, uri)
        {
            Content = new ByteArrayContent(body)
        };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var secret = await ResolveSigningSecretAsync(request.BusinessKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return message;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var signature = Sign(method.Method, uri.AbsolutePath, timestamp, nonce, body, secret);
        message.Headers.TryAddWithoutValidation("X-Harbor-AI-Key", producerKey);
        message.Headers.TryAddWithoutValidation("X-Harbor-AI-Timestamp", timestamp);
        message.Headers.TryAddWithoutValidation("X-Harbor-AI-Nonce", nonce);
        message.Headers.TryAddWithoutValidation("X-Harbor-AI-Signature", signature);
        return message;
    }

    /// <summary>
    /// 解析签名密钥。
    /// </summary>
    private async Task<string?> ResolveSigningSecretAsync(string businessKey, CancellationToken cancellationToken)
    {
        if (businessSigningSecretResolver is not null && !string.IsNullOrWhiteSpace(businessKey))
        {
            var businessInfo = await businessSigningSecretResolver.ResolveAsync(businessKey, cancellationToken);
            if (businessInfo is not null)
            {
                if (!string.IsNullOrWhiteSpace(businessInfo.Secret))
                {
                    return businessInfo.Secret;
                }

                // 业务已绑定 SigningSecretRef 时，禁止回退到全局密钥，避免 Worker 侧校验失败。
                if (businessInfo.RequiresSignature)
                {
                    return null;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(options.Value.SigningSecret))
        {
            return options.Value.SigningSecret;
        }

        if (string.IsNullOrWhiteSpace(options.Value.SigningSecretRef))
        {
            return null;
        }

        foreach (var resolver in secretResolvers)
        {
            var secret = await resolver.ResolveAsync(options.Value.SigningSecretRef, cancellationToken);
            if (!string.IsNullOrWhiteSpace(secret))
            {
                return secret;
            }
        }

        return Environment.GetEnvironmentVariable(options.Value.SigningSecretRef);
    }

    /// <summary>
    /// 计算签名。
    /// </summary>
    public static string Sign(string method, string path, string timestamp, string nonce, byte[] body, string secret)
    {
        var bodyHash = Convert.ToBase64String(SHA256.HashData(body));
        var payload = string.Join('\n', method.ToUpperInvariant(), path, timestamp, nonce, bodyHash);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
using System.Collections.Concurrent;
using System.Security.Cryptography;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// AIWorker 内部请求签名校验器。
/// </summary>
public sealed class AiRequestSignatureValidator(AiRuntimeConfigCache configCache, ISecretResolver secretResolver)
{
    private static readonly TimeSpan MaxSkew = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seenNonces = new(StringComparer.Ordinal);

    /// <summary>
    /// 校验请求签名。
    /// </summary>
    public async Task<AiSignatureValidationResult> ValidateAsync(
        HttpRequest httpRequest,
        byte[] body,
        AiBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await configCache.GetCurrentAsync(cancellationToken);
        var business = snapshot?.Businesses.FirstOrDefault(b => string.Equals(b.BusinessKey, request.BusinessKey, StringComparison.Ordinal));
        if (business is null)
        {
            // 未配置签名的未知业务交给后续业务校验返回 BusinessNotFound，避免签名层泄露业务存在性。
            return AiSignatureValidationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(business.SigningSecretRef))
        {
            // 未配置业务签名密钥时跳过签名校验，便于管理端调试未启用签名的业务。
            return AiSignatureValidationResult.Success();
        }

        var producerKey = FirstHeader(httpRequest, "X-Harbor-AI-Key");
        var timestamp = FirstHeader(httpRequest, "X-Harbor-AI-Timestamp");
        var nonce = FirstHeader(httpRequest, "X-Harbor-AI-Nonce");
        var signature = FirstHeader(httpRequest, "X-Harbor-AI-Signature");
        if (string.IsNullOrWhiteSpace(producerKey) || string.IsNullOrWhiteSpace(timestamp) ||
            string.IsNullOrWhiteSpace(nonce) || string.IsNullOrWhiteSpace(signature))
        {
            return AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI request signature headers are required.");
        }

        if (!string.Equals(producerKey, request.ProducerKey, StringComparison.Ordinal))
        {
            return AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI producer key does not match signed header.");
        }

        if (!long.TryParse(timestamp, out var unixSeconds))
        {
            return AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI request timestamp is invalid.");
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if (DateTimeOffset.UtcNow - requestTime > MaxSkew || requestTime - DateTimeOffset.UtcNow > MaxSkew)
        {
            return AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI request timestamp is expired.");
        }

        CleanupNonces();
        if (!_seenNonces.TryAdd($"{producerKey}:{nonce}", requestTime))
        {
            return AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI request nonce was already used.");
        }

        var secret = await secretResolver.ResolveAsync(business.SigningSecretRef, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI signing secret was not configured.");
        }

        var expected = AiRequestSigner.Sign(httpRequest.Method, httpRequest.Path.Value ?? "/", timestamp, nonce, body, secret);
        return FixedEquals(expected, signature)
            ? AiSignatureValidationResult.Success()
            : AiSignatureValidationResult.Failed(AiErrorCodes.InvalidSignature, "AI request signature is invalid.");
    }

    /// <summary>
    /// 读取指定 Header 的第一个值。
    /// </summary>
    private static string? FirstHeader(HttpRequest request, string name) =>
        request.Headers.TryGetValue(name, out var values) ? values.FirstOrDefault() : null;

    /// <summary>
    /// 清理超过允许时间偏差的 nonce 记录。
    /// </summary>
    private void CleanupNonces()
    {
        var expiredBefore = DateTimeOffset.UtcNow.Subtract(MaxSkew);
        foreach (var (key, seenAt) in _seenNonces)
        {
            if (seenAt < expiredBefore)
            {
                _seenNonces.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 用固定时间比较 Base64 签名。
    /// </summary>
    private static bool FixedEquals(string expected, string actual)
    {
        try
        {
            var expectedBytes = Convert.FromBase64String(expected);
            var actualBytes = Convert.FromBase64String(actual);
            return actualBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// AI 签名校验结果。
/// </summary>
public sealed record AiSignatureValidationResult(bool Valid, string? ErrorCode = null, string? ErrorMessage = null)
{
    /// <summary>
    /// 成功。
    /// </summary>
    public static AiSignatureValidationResult Success() => new(true);

    /// <summary>
    /// 失败。
    /// </summary>
    public static AiSignatureValidationResult Failed(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}

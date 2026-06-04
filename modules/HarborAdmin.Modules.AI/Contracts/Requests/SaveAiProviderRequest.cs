namespace HarborAdmin.Modules.AI.Contracts.Requests;

/// <summary>
/// 保存 AI 供应商请求。
/// </summary>
public sealed record SaveAiProviderRequest(
    string ProviderKey,
    string DisplayName,
    string AdapterType,
    string BaseUrl,
    string? SecretRef,
    string? DefaultHeadersJson,
    string? DefaultBodyJson,
    bool Enabled,
    bool SupportsStreaming,
    int TimeoutSeconds,
    int MaxRetryCount,
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerBreakSeconds,
    IReadOnlyList<SaveAiProviderModelRequest> Models);

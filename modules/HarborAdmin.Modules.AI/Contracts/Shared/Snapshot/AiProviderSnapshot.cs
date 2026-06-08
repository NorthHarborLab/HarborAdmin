namespace HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;

/// <summary>
/// 已发布供应商。
/// </summary>
public sealed record AiProviderSnapshot(
    string ProviderKey,
    string DisplayName,
    string AdapterType,
    string BaseUrl,
    string? SecretRef,
    int SecretVersion,
    string? DefaultHeadersJson,
    string? DefaultBodyJson,
    bool SupportsStreaming,
    int TimeoutSeconds,
    int MaxRetryCount,
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerBreakSeconds,
    IReadOnlyList<AiProviderModelSnapshot> Models);

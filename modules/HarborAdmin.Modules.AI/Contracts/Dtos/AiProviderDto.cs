namespace HarborAdmin.Modules.AI.Contracts.Dtos;

/// <summary>
/// AI 供应商 DTO。
/// </summary>
public sealed record AiProviderDto(
    long Id,
    string ProviderKey,
    string DisplayName,
    string AdapterType,
    string BaseUrl,
    string? SecretRef,
    int SecretVersion,
    bool SecretConfigured,
    string? DefaultHeadersJson,
    string? DefaultBodyJson,
    bool Enabled,
    bool SupportsStreaming,
    int TimeoutSeconds,
    int MaxRetryCount,
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerBreakSeconds,
    IReadOnlyList<AiProviderModelDto> Models,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 供应商实例。
/// </summary>
[Index("ux_ai_provider_key", nameof(ProviderKey), true)]
public sealed class AiProvider : AuditableEntity
{
    /// <summary>
    /// 供应商实例 Key。
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 协议适配类型。
    /// </summary>
    public string AdapterType { get; set; } = string.Empty;

    /// <summary>
    /// 基础地址。
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 密钥引用。
    /// </summary>
    public string? SecretRef { get; set; }

    /// <summary>
    /// 密钥版本。
    /// </summary>
    public int SecretVersion { get; set; }

    /// <summary>
    /// 默认 Header JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? DefaultHeadersJson { get; set; }

    /// <summary>
    /// 默认 Body 扩展 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? DefaultBodyJson { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 是否支持流式。
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// 请求超时秒数。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 熔断失败阈值。
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    /// <summary>
    /// 熔断恢复秒数。
    /// </summary>
    public int CircuitBreakerBreakSeconds { get; set; } = 60;

    /// <summary>
    /// 供应商模型。
    /// </summary>
    [Navigate(nameof(AiProviderModel.ProviderId))]
    public List<AiProviderModel> Models { get; set; } = [];
}

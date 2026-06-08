using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Provider.Request;

/// <summary>
/// 保存 AI 供应商请求。
/// </summary>
public sealed class SaveAiProviderRequest
{
    /// <summary>
    /// 供应商 Key。
    /// </summary>
    [Required(ErrorMessage = "供应商 Key 不能为空。")]
    [MaxLength(64)]
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    [Required(ErrorMessage = "显示名称不能为空。")]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 协议适配类型。
    /// </summary>
    [Required(ErrorMessage = "适配类型不能为空。")]
    [MaxLength(64)]
    public string AdapterType { get; set; } = string.Empty;

    /// <summary>
    /// 基础地址。
    /// </summary>
    [Required(ErrorMessage = "基础地址不能为空。")]
    [MaxLength(500)]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 密钥引用。
    /// </summary>
    [MaxLength(128)]
    public string? SecretRef { get; set; }

    /// <summary>
    /// 默认 Header JSON。
    /// </summary>
    public string? DefaultHeadersJson { get; set; }

    /// <summary>
    /// 默认 Body 扩展 JSON。
    /// </summary>
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
    [Range(0, 3600, ErrorMessage = "请求超时秒数不合法。")]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    [Range(0, 100, ErrorMessage = "最大重试次数不合法。")]
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 熔断失败阈值。
    /// </summary>
    [Range(0, 1000, ErrorMessage = "熔断失败阈值不合法。")]
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    /// <summary>
    /// 熔断恢复秒数。
    /// </summary>
    [Range(0, 3600, ErrorMessage = "熔断恢复秒数不合法。")]
    public int CircuitBreakerBreakSeconds { get; set; } = 60;

    /// <summary>
    /// 供应商模型列表。
    /// </summary>
    public List<SaveAiProviderModelRequest> Models { get; set; } = [];
}

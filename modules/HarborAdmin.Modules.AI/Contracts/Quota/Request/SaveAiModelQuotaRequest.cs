using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Quota.Request;

/// <summary>
/// 保存 AI 模型限额请求。
/// </summary>
public sealed class SaveAiModelQuotaRequest
{
    /// <summary>
    /// 供应商 Key。
    /// </summary>
    [Required(ErrorMessage = "供应商 Key 不能为空。")]
    [MaxLength(64)]
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称。
    /// </summary>
    [MaxLength(120)]
    public string? ModelName { get; set; }

    /// <summary>
    /// 端点 Key。
    /// </summary>
    [MaxLength(64)]
    public string? EndpointKey { get; set; }

    /// <summary>
    /// 业务 Key。
    /// </summary>
    [MaxLength(64)]
    public string? BusinessKey { get; set; }

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    [MaxLength(64)]
    public string? ProducerKey { get; set; }

    /// <summary>
    /// 每分钟请求数限额。
    /// </summary>
    public int? RequestsPerMinute { get; set; }

    /// <summary>
    /// 每分钟 Token 限额。
    /// </summary>
    public int? TokensPerMinute { get; set; }

    /// <summary>
    /// 每日请求数限额。
    /// </summary>
    public int? RequestsPerDay { get; set; }

    /// <summary>
    /// 每日 Token 限额。
    /// </summary>
    public int? TokensPerDay { get; set; }

    /// <summary>
    /// 每月预算。
    /// </summary>
    public decimal? MonthlyBudget { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

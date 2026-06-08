using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Provider.Request;

/// <summary>
/// 保存 AI 供应商限额请求。
/// </summary>
public sealed class SaveAiProviderQuotaRequest
{
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
    /// 每日请求数限额。
    /// </summary>
    public int? RequestsPerDay { get; set; }

    /// <summary>
    /// 每日 Token 限额。
    /// </summary>
    public int? TokensPerDay { get; set; }

    /// <summary>
    /// 每月 Token 限额。
    /// </summary>
    public int? TokensPerMonth { get; set; }

    /// <summary>
    /// 每月预算。
    /// </summary>
    public decimal? MonthlyBudget { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}

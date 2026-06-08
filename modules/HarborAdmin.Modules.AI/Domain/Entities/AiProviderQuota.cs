using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 供应商限额。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_provider_quota", $"{nameof(ProviderId)},{nameof(ProducerKey)}", true)]
public sealed class AiProviderQuota : EntityBase
{
    /// <summary>
    /// 供应商主键。
    /// </summary>
    public long ProviderId { get; set; }

    /// <summary>
    /// 调用方 Key，空表示全部调用方。
    /// </summary>
    public string? ProducerKey { get; set; }

    /// <summary>
    /// 每分钟请求数。
    /// </summary>
    public int? RequestsPerMinute { get; set; }

    /// <summary>
    /// 每日请求数。
    /// </summary>
    public int? RequestsPerDay { get; set; }

    /// <summary>
    /// 每日 Token。
    /// </summary>
    public int? TokensPerDay { get; set; }

    /// <summary>
    /// 每月 Token。
    /// </summary>
    public int? TokensPerMonth { get; set; }

    /// <summary>
    /// 月度预算。
    /// </summary>
    public decimal? MonthlyBudget { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 所属供应商。
    /// </summary>
    [Navigate(nameof(ProviderId))]
    public AiProvider? Provider { get; set; }
}


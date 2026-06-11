using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 模型限额。
/// </summary>
[Index("ux_ai_model_quota", $"{nameof(ProviderKey)},{nameof(ModelName)},{nameof(BusinessKey)},{nameof(ProducerKey)}", true)]
public sealed class AiModelQuota : EntityBase
{
    /// <summary>
    /// 供应商 Key。
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称。
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// 端点 Key。
    /// </summary>
    public string? EndpointKey { get; set; }

    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string? BusinessKey { get; set; }

    /// <summary>
    /// 调用方 Key，空表示全部调用方。
    /// </summary>
    public string? ProducerKey { get; set; }

    /// <summary>
    /// 每分钟请求数。
    /// </summary>
    public int? RequestsPerMinute { get; set; }

    /// <summary>
    /// 每分钟 Token。
    /// </summary>
    public int? TokensPerMinute { get; set; }

    /// <summary>
    /// 每日请求数。
    /// </summary>
    public int? RequestsPerDay { get; set; }

    /// <summary>
    /// 每日 Token。
    /// </summary>
    public int? TokensPerDay { get; set; }

    /// <summary>
    /// 月度预算。
    /// </summary>
    public decimal? MonthlyBudget { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}


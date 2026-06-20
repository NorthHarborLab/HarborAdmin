using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.Modules.AI.Contracts.Observability.Request;

/// <summary>
/// AI 用量汇总查询。
/// </summary>
public sealed class AiUsageSummaryQuery : HarborQueryOptions
{
    /// <summary>
    /// 区间开始（含）。
    /// </summary>
    public DateTimeOffset? DateFrom { get; set; }

    /// <summary>
    /// 区间结束（含当日）。
    /// </summary>
    public DateTimeOffset? DateTo { get; set; }

    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string? BusinessKey { get; set; }

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    public string? ProducerKey { get; set; }

    /// <summary>
    /// 供应商 Key。
    /// </summary>
    public string? ProviderKey { get; set; }

    /// <summary>
    /// 模型名称。
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 聚合维度：day / business / provider。
    /// </summary>
    public string GroupBy { get; set; } = "day";
}

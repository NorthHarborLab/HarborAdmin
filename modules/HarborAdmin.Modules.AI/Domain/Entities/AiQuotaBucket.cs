using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 配额窗口桶。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_quota_bucket", "ProviderKey,Model,BusinessKey,ProducerKey,WindowType,WindowStart", true)]
public class AiQuotaBucket : EntityBase
{
    /// <summary>
    /// 供应商 Key。
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型。
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 业务 Key。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 调用方 Key。
    /// </summary>
    public string ProducerKey { get; set; } = string.Empty;

    /// <summary>
    /// 窗口类型。
    /// </summary>
    public string WindowType { get; set; } = "Day";

    /// <summary>
    /// 窗口开始时间。
    /// </summary>
    public DateTimeOffset WindowStart { get; set; }

    /// <summary>
    /// 预占请求数。
    /// </summary>
    public int ReservedRequests { get; set; }

    /// <summary>
    /// 成功请求数。
    /// </summary>
    public int SuccessRequests { get; set; }

    /// <summary>
    /// 失败请求数。
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// 总 Token。
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 成本。
    /// </summary>
    public decimal Cost { get; set; }
}

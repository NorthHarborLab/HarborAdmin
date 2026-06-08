using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 业务供应商路由。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_business_route", $"{nameof(BusinessId)},{nameof(ProviderKey)},{nameof(Priority)}", true)]
public sealed class AiBusinessProviderRoute : EntityBase
{
    /// <summary>
    /// 业务主键。
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// 供应商 Key。
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型覆盖。
    /// </summary>
    public string? ModelOverride { get; set; }

    /// <summary>
    /// 优先级。
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 路由供应商选项 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? ProviderOptionsJson { get; set; }

    /// <summary>
    /// OpenRouter 路由选项 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? OpenRouterOptionsJson { get; set; }

    /// <summary>
    /// 所属业务。
    /// </summary>
    [Navigate(nameof(BusinessId))]
    public AiBusiness? Business { get; set; }
}


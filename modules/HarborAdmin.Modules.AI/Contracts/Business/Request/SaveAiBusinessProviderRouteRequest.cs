using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Business.Request;

/// <summary>
/// 保存 AI 业务供应商路由请求。
/// </summary>
public sealed class SaveAiBusinessProviderRouteRequest
{
    /// <summary>
    /// 供应商 Key。
    /// </summary>
    [Required(ErrorMessage = "供应商 Key 不能为空。")]
    [MaxLength(64)]
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型覆盖。
    /// </summary>
    [MaxLength(120)]
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
    /// 供应商选项 JSON。
    /// </summary>
    public string? ProviderOptionsJson { get; set; }

    /// <summary>
    /// OpenRouter 选项 JSON。
    /// </summary>
    public string? OpenRouterOptionsJson { get; set; }
}

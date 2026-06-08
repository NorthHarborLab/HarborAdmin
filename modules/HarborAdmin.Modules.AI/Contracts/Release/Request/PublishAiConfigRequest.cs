using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Release.Request;

/// <summary>
/// 发布 AI 配置请求。
/// </summary>
public sealed class PublishAiConfigRequest
{
    /// <summary>
    /// 发布人。
    /// </summary>
    [MaxLength(64)]
    public string? PublishedBy { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }
}

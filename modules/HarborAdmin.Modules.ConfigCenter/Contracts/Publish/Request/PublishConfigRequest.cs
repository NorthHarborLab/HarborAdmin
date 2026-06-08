using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Request;

/// <summary>
/// 发布操作请求。
/// </summary>
public sealed class PublishConfigRequest
{
    /// <summary>
    /// 发布人。
    /// </summary>
    [MaxLength(64)]
    public string? PublishedBy { get; set; }
}
